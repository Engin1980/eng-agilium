using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Exceptions.Validation;
using Eng.Agilium.Be.Model.Db;
using Eng.Agilium.Be.Services;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Auth.Login;

public record Command(
  [property: XEmailValidation] string Email,
  [property: XRegex(".+", false, XValidationErrorKey.INVALID_PASSWORD_FORMAT)] string Password,
  bool RememberMe,
  string TurnstileToken
);

public class Handler(AppDbContext dbContext, TokenService tokenService, AppSettingsService appSettingsService)
  : GenericHandler<Command, EmptyParameters, (string, AppUser)>
{
  private readonly TokenService tokenService = tokenService;
  private readonly AppSettings appSettings = appSettingsService.AppSettings;

  public override async Task<(string, AppUser)> HandleAsync(
    Command request,
    EmptyParameters parameters,
    CancellationToken cancellationToken
  )
  {
    var email = request.Email.Trim().ToLowerInvariant();
    var password = request.Password;
    var appUser =
      await dbContext
        .AppUsers //
        .AsNoTracking()
        .Include(q => q.Memberships)
        .FirstOrDefaultAsync(u => u.Email == email, cancellationToken: cancellationToken)
      ?? throw new AuthenticationFailedException(AuthenticationFailedException.FailureReason.EmailNotFound, null);

    if (appUser.IsActive == false)
      throw new AccountDeactivatedException();

    if (appUser.FailedLoginAttemptsCount >= appSettings.Security.Password.MaxFailedLoginAttempts)
      throw new AccountLockedException();

    if (string.IsNullOrEmpty(appUser.PasswordHash) || !VerifyPassword(password, appUser.PasswordHash))
    {
      appUser.FailedLoginAttemptsCount++;
      await dbContext.SaveChangesAsync(cancellationToken);
      throw new AuthenticationFailedException(AuthenticationFailedException.FailureReason.CredentialsInvalid, appUser);
    }

    appUser.FailedLoginAttemptsCount = 0;
    await dbContext.SaveChangesAsync(cancellationToken);

    string refreshToken = await tokenService.CreateRefreshAsync(appUser);
    return (refreshToken, appUser);
  }

  protected static bool VerifyPassword(string password, string passwordHash) =>
    BCrypt.Net.BCrypt.Verify(password, passwordHash);
}

[EndpointSummary(
  "Logs in an user and returns an access token. Also sets a refresh token cookie if login is successful."
)]
[EndpointDescription(
  "This endpoint allows an user to log in using their email and password. If the login is successful, an access token is returned in the response body "
    + "and a refresh token is set in an HttpOnly cookie. The access token can be used for authenticating subsequent requests, while the refresh token can be "
    + "used to obtain new access tokens when the current one expires."
)]
public class EndPoint(AppSettingsService appSettingsService, TurnstileService turnstileService)
  : GenericEndpoint<Command, EmptyParameters, Handler>
{
  private readonly AppSettingsService appSettingsService = appSettingsService;
  private readonly TurnstileService turnstileService = turnstileService;
  private readonly AppSettings appSettings = appSettingsService.AppSettings;

  public override HttpMethod Method => HttpMethod.Post;

  public override BaseRoute BaseRoute => BaseRoute.Auth;

  public override string EndpointRoute => "/login/admin";

  public override string[] RequiredRoles => [];

  protected override async Task<IResult> ProcessRequestAsync(
    Command command,
    EmptyParameters parameters,
    HttpContext httpContext,
    Handler handler,
    CancellationToken cancellationToken
  )
  {
    if (appSettings.Security.Turnstile.Enabled)
    {
      var turnstileResponse = await turnstileService.ValidateTurnstileTokenAsync(httpContext, command.TurnstileToken);
      if (!turnstileResponse.Success)
      {
        throw new TurnstileVerificationFailedException(turnstileResponse.ErrorCodes);
      }
    }

    var (refreshToken, appUser) = await handler.HandleAsync(command, parameters, cancellationToken);

    string[] roles = appUser.Memberships.Select(m => ((IRoleAssignment)m.Role).ToRoleString()).ToArray();

    var jwtToken = Utils.GenerateJwtToken(
      appUser.Id,
      appUser.Email,
      appUser.Name,
      appUser.Surname,
      roles,
      appSettings.Security.Jwt.Key,
      appSettings.Security.Jwt.Issuer,
      appSettings.Security.Tokens.AccessTokenExpirationMinutes
    );
    Utils.AppendRefreshTokenCookie(
      refreshToken,
      appSettings.Security.Tokens.RefreshTokenExpirationMinutes,
      httpContext
    );
    return Results.Ok(jwtToken);
  }
}

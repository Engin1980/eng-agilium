using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Model.Db;
using Eng.Agilium.Be.Services;

namespace Eng.Agilium.Be.Features.Auth.Refresh;

public record Result(string RefreshToken, int AppUserId, string Email, string Name, string Surname, string Role);

public class Handler(TokenService tokenService) : GenericHandler<EmptyCommand, EmptyParameters, (string, AppUser)>
{
  public string RefreshToken { get; set; } = string.Empty;

  public override async Task<(string, AppUser)> HandleAsync(
    EmptyCommand request,
    EmptyParameters parameters,
    CancellationToken cancellationToken
  )
  {
    var appUser =
      await tokenService.ObtainAppUserByTokenAsync(this.RefreshToken, TokenType.Refresh, true)
      ?? throw new AuthenticationFailedException(AuthenticationFailedException.FailureReason.TokenNotFound, null);

    string refreshToken = await tokenService.CreateRefreshAsync(appUser);

    return (refreshToken, appUser);
  }
}

[EndpointSummary("Refreshes the access token using a valid refresh token. ")]
[EndpointDescription(
  """
  The refresh token is provided in an HTTP-only cookie and is used to verify the user's identity and permissions. 
  If the refresh token is valid, a new access token is generated and returned in the response. The refresh token 
  is also renewed, extending its validity period, and the old one is revoked.
"""
)]
public class EndPoint(AppSettingsService appSettingsService) : GenericEndpoint<EmptyCommand, EmptyParameters, Handler>
{
  private readonly AppSettingsService appSettingsService = appSettingsService;

  public override HttpMethod Method => HttpMethod.Post;

  public override BaseRoute BaseRoute => BaseRoute.Auth;

  public override string EndpointRoute => "/refresh";

  public override string[] RequiredRoles => [];

  protected override async Task<IResult> ProcessRequestAsync(
    EmptyCommand command,
    EmptyParameters parameters,
    HttpContext httpContext,
    Handler handler,
    CancellationToken cancellationToken
  )
  {
    string refreshToken =
      httpContext.Request.Cookies["refreshToken"]
      ?? throw new AuthenticationFailedException(AuthenticationFailedException.FailureReason.TokenNotProvided, null);
    handler.RefreshToken = refreshToken;
    var (newRefreshToken, appUser) = await handler.HandleAsync(
      new EmptyCommand(),
      new EmptyParameters(),
      cancellationToken
    );

    string[] roles = appUser.Memberships.Select(m => ((IRoleAssignment)m.Role).ToRoleString()).ToArray();

    var jwtToken = Utils.GenerateJwtToken(
      appUser.Id,
      appUser.Email,
      appUser.Name,
      appUser.Surname,
      roles,
      appSettingsService.AppSettings.Security.Jwt.Key,
      appSettingsService.AppSettings.Security.Jwt.Issuer,
      appSettingsService.AppSettings.Security.Tokens.AccessTokenExpirationMinutes
    );
    Utils.AppendRefreshTokenCookie(
      newRefreshToken,
      appSettingsService.AppSettings.Security.Tokens.RefreshTokenExpirationMinutes,
      httpContext
    );

    return Results.Ok(jwtToken);
  }
}

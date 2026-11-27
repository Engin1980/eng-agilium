using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Exceptions.Validation;
using Eng.Agilium.Be.Model.Db;
using Eng.Agilium.Be.Services;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Auth.ChangePassword;

public record Command(
  [property: XRegex(".+", false, XValidationErrorKey.INVALID_PASSWORD_FORMAT)] string CurrentPassword,
  [property: XRegex(".{8,}", false, XValidationErrorKey.INVALID_PASSWORD_FORMAT)] string NewPassword
);

public class Handler(AppDbContext dbContext, TokenService tokenService, IHttpContextAccessor httpContextAccessor)
  : GenericHandler<Command, EmptyParameters, EmptyResult>
{
  public override async Task<EmptyResult> HandleAsync(
    Command command,
    EmptyParameters parameters,
    CancellationToken cancellationToken
  )
  {
    var userId = LoggedUser?.AppUserId ?? throw new InvalidOperationException("Login is required.");
    var user = await dbContext.AppUsers.FirstAsync(u => u.Id == userId, cancellationToken);

    // 400, not 401: the FE treats 401 as an expired session
    if (!BCrypt.Net.BCrypt.Verify(command.CurrentPassword, user.PasswordHash))
      throw new BadRequestException("The current password is not correct.");

    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(command.NewPassword);
    await dbContext.SaveChangesAsync(cancellationToken);

    // other devices have to log in again; this session (its refresh cookie) stays
    var current = httpContextAccessor.HttpContext?.Request.Cookies["refreshToken"];
    await tokenService.RevokeRefreshTokensAsync(userId, current);
    return new EmptyResult();
  }
}

[EndpointSummary("Changes the password of the logged-in user (other sessions are signed out)")]
public class Endpoint : GenericOkEndpoint<Command, EmptyParameters, Handler, EmptyResult>
{
  public override HttpMethod Method => HttpMethod.Post;
  public override BaseRoute BaseRoute => BaseRoute.Auth;
  public override string EndpointRoute => "password";
  public override EndpointPolicy Policy => EndpointPolicy.LoggedIn;
}

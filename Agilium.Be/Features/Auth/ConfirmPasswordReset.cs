using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Exceptions.Validation;
using Eng.Agilium.Be.Model.Db;
using Eng.Agilium.Be.Services;

namespace Eng.Agilium.Be.Features.Auth.ConfirmPasswordReset;

public record Command(
  [property: XTokenValidation] string Token,
  [property: XRegex(".{8,}", false, XValidationErrorKey.INVALID_PASSWORD_FORMAT)] string NewPassword
);

public class Handler(AppDbContext dbContext, TokenService tokenService)
  : GenericHandler<Command, EmptyParameters, EmptyResult>
{
  public override async Task<EmptyResult> HandleAsync(
    Command command,
    EmptyParameters parameters,
    CancellationToken cancellationToken
  )
  {
    var token =
      await tokenService.FindValidAsync(command.Token, TokenType.PasswordReset, cancellationToken)
      ?? throw new InvalidTokenException();

    var user = token.AppUser;
    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(command.NewPassword);
    user.FailedLoginAttemptsCount = 0;
    user.IsLockedUntil = null;
    // the reset proves that the owner reads the mailbox
    user.EmailVerifiedAt ??= DateTime.UtcNow;
    dbContext.Tokens.Remove(token); // single use
    await dbContext.SaveChangesAsync(cancellationToken);

    // every existing session must log in again with the new password
    await tokenService.RevokeRefreshTokensAsync(user.Id);
    return new EmptyResult();
  }
}

[EndpointSummary("Sets a new password with the token from the reset e-mail (single use; signs the user out everywhere)")]
public class Endpoint : GenericOkEndpoint<Command, EmptyParameters, Handler, EmptyResult>
{
  public override HttpMethod Method => HttpMethod.Post;
  public override BaseRoute BaseRoute => BaseRoute.Auth;
  public override string EndpointRoute => "password-reset/confirm";
  public override EndpointPolicy Policy => EndpointPolicy.Anonymous;
}

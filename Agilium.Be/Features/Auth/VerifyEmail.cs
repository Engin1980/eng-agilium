using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Exceptions.Validation;
using Eng.Agilium.Be.Model.Db;
using Eng.Agilium.Be.Services;

namespace Eng.Agilium.Be.Features.Auth.VerifyEmail;

public record Command([property: XTokenValidation] string Token);

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
      await tokenService.FindValidAsync(command.Token, TokenType.EmailVerification, cancellationToken)
      ?? throw new InvalidTokenException();

    token.AppUser.EmailVerifiedAt ??= DateTime.UtcNow;
    dbContext.Tokens.Remove(token); // single use
    await dbContext.SaveChangesAsync(cancellationToken);
    return new EmptyResult();
  }
}

[EndpointSummary("Confirms an e-mail address with the token from the verification e-mail (single use)")]
public class Endpoint : GenericOkEndpoint<Command, EmptyParameters, Handler, EmptyResult>
{
  public override HttpMethod Method => HttpMethod.Post;
  public override BaseRoute BaseRoute => BaseRoute.Auth;
  public override string EndpointRoute => "verify-email";
  public override EndpointPolicy Policy => EndpointPolicy.Anonymous;
}

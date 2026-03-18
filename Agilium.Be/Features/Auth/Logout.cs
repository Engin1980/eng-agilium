using Eng.Agilium.Be.Services;

namespace Eng.Agilium.Be.Features.Auth.Logout;

public record Command(string RefreshToken);

public class Handler(TokenService tokenService) : GenericHandler<Command, EmptyParameters, EmptyResult>
{
  private readonly TokenService tokenService = tokenService;

  public override async Task<EmptyResult> HandleAsync(
    Command request,
    EmptyParameters parameters,
    CancellationToken cancellationToken
  )
  {
    if (request.RefreshToken != null)
      await tokenService.RevokeRefreshTokenAsync(request.RefreshToken);

    return new EmptyResult();
  }
}

[EndpointSummary("Logs out an user and revokes the refresh token.")]
public class EndPoint : GenericEndpoint<Command, EmptyParameters, Handler>
{
  public override HttpMethod Method => HttpMethod.Post;

  public override BaseRoute BaseRoute => BaseRoute.Auth;

  public override string EndpointRoute => "/logout";

  public override string[] RequiredRoles => [];

  protected override async Task<IResult> ProcessRequestAsync(
    Command command,
    EmptyParameters parameters,
    HttpContext httpContext,
    Handler handler,
    CancellationToken cancellationToken
  )
  {
    await handler.HandleAsync(command, parameters, cancellationToken);

    Utils.AppendRefreshTokenCookie("", -1, httpContext);

    return Results.NoContent();
  }
}

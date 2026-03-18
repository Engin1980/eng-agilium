namespace Eng.Agilium.Be.Features.Health.Ping;
public class Handler : GenericHandler<EmptyCommand, EmptyParameters, EmptyResult>
{
  public override Task<EmptyResult> HandleAsync(
    EmptyCommand request,
    EmptyParameters parameters,
    CancellationToken cancellationToken
  )
  {
    return Task.FromResult(new EmptyResult());
  }
}

[EndpointSummary("Checks if the backend is reachable.")]
public class Endpoint : GenericOkEndpoint<EmptyCommand, EmptyParameters, Handler, EmptyResult>
{
  public override HttpMethod Method => HttpMethod.Get;

  public override BaseRoute BaseRoute => BaseRoute.Health;

  public override string EndpointRoute => "ping";

  public override string[] RequiredRoles => [];
}

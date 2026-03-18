using Eng.Agilium.Be.Model.Db;

namespace Eng.Agilium.Be.Features.Projects.StateOptions;

public record OptionResult(int Id, string Title);

public record Result(IEnumerable<OptionResult> Options);

public class Handler : GenericHandler<EmptyCommand, EmptyParameters, Result>
{
  public override Task<Result> HandleAsync(
    EmptyCommand command,
    EmptyParameters parameters,
    CancellationToken cancellationToken
  )
  {
    var options = Enum.GetValues(typeof(ProjectState))
      .Cast<ProjectState>()
      .Select(s => new OptionResult((int)s, s.ToString()))
      .ToList();

    return Task.FromResult(new Result(options));
  }
}

[EndpointSummary("Returns available project states")]
public class Endpoint : GenericOkEndpoint<EmptyCommand, EmptyParameters, Handler, Result>
{
  public override HttpMethod Method => HttpMethod.Get;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "state-options";
  public override string[] RequiredRoles => [];
}

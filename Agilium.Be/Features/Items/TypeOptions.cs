using Eng.Agilium.Be.Model.Db;

namespace Eng.Agilium.Be.Features.Items.TypeOptions;

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
    var options = Enum.GetValues(typeof(ItemType))
      .Cast<ItemType>()
      .Select(t => new OptionResult((int)t, t.ToString()))
      .ToList();

    return Task.FromResult(new Result(options));
  }
}

[EndpointSummary("Returns available Item types")]
public class Endpoint : GenericOkEndpoint<EmptyCommand, EmptyParameters, Handler, Result>
{
  public override HttpMethod Method => HttpMethod.Get;
  public override BaseRoute BaseRoute => BaseRoute.Items;
  public override string EndpointRoute => "type-options";
  public override string[] RequiredRoles => [];
}

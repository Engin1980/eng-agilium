using System.Diagnostics;
using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Model.Db;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Health.Db;

public record Result(int ResponseTimeMs);

public class Handler(AppDbContext dbContext) : GenericHandler<EmptyCommand, EmptyParameters, Result>
{
  public override async Task<Result> HandleAsync(
    EmptyCommand request,
    EmptyParameters parameters,
    CancellationToken cancellationToken
  )
  {
    var sw = Stopwatch.StartNew();
    try
    {
      await dbContext.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
    }
    catch (Exception ex)
    {
      throw new ServiceUnreachableException(ServiceType.Database, ex);
    }
    finally
    {
      sw.Stop();
    }

    return new Result((int)sw.ElapsedMilliseconds);
  }
}

[EndpointSummary("Checks if the database is reachable and measures the response time.")]
public class EndPoint : GenericOkEndpoint<EmptyCommand, EmptyParameters, Handler, Result>
{
  public override HttpMethod Method => HttpMethod.Get;

  public override BaseRoute BaseRoute => BaseRoute.Health;

  public override string EndpointRoute => "/db";

  public override string[] RequiredRoles => [];
}

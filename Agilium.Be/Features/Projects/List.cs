namespace Eng.Agilium.Be.Features.Projects.List;

using Eng.Agilium.Be.Model.Db;
using Eng.Agilium.Be.Services;
using Microsoft.EntityFrameworkCore;

public record ProjectResult(int Id, string Title, string Description, int Status, int MemberCount);

public record Result(IEnumerable<ProjectResult> Projects);

public class Handler(AppDbContext dbContext) : GenericHandler<EmptyCommand, EmptyParameters, Result>
{
  public override async Task<Result> HandleAsync(
    EmptyCommand command,
    EmptyParameters parameters,
    CancellationToken cancellationToken
  )
  {
    var userId = LoggedUser?.AppUserId ?? throw new InvalidOperationException("Login is required.");

    var projects = await dbContext
      .Projects //
      .AsNoTracking()
      .Where(p => p.Memberships.Any(m => m.UserId == userId))
      .OrderBy(p => p.State)
      .ThenBy(p => p.Title)
      .Select(p => new ProjectResult(p.Id, p.Title, p.Description, (int)p.State, p.Memberships.Count))
      .ToListAsync(cancellationToken);

    return new Result(projects);
  }
}

[EndpointSummary("Returns the projects the logged-in user is a member of")]
public class Endpoint : GenericOkEndpoint<EmptyCommand, EmptyParameters, Handler, Result>
{
  public override HttpMethod Method => HttpMethod.Get;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "";
  public override EndpointPolicy Policy => EndpointPolicy.LoggedIn;
}

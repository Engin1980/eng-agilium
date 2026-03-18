namespace Eng.Agilium.Be.Features.Projects.List;

using Eng.Agilium.Be.Model.Db;
using Microsoft.EntityFrameworkCore;

public record MemberIdParameters(int? MemberId);

public record ProjectResult(int Id, string Title, string Description, int Status, int MemberCount);

public record Result(IEnumerable<ProjectResult> Projects);

public class Handler(AppDbContext dbContext) : GenericHandler<EmptyCommand, MemberIdParameters, Result>
{
  public override async Task<Result> HandleAsync(
    EmptyCommand command,
    MemberIdParameters parameters,
    CancellationToken cancellationToken
  )
  {
    var query = dbContext.Projects.AsNoTracking();

    if (parameters?.MemberId is int mId)
      query = query.Where(p => p.Memberships.Any(m => m.UserId == mId));

    var projects = await query
      .OrderBy(p => p.Status)
      .ThenBy(p => p.Title)
      .Select(p => new ProjectResult(p.Id, p.Title, p.Description, (int)p.Status, p.Memberships.Count))
      .ToListAsync(cancellationToken);

    return new Result(projects);
  }
}

[EndpointSummary("Returns projects; optionally filtered by memberId via query parameter")]
public class Endpoint : GenericOkEndpoint<EmptyCommand, MemberIdParameters, Handler, Result>
{
  public override HttpMethod Method => HttpMethod.Get;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "";
  public override string[] RequiredRoles => [];
}

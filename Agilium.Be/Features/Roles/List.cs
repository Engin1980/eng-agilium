using Eng.Agilium.Be.Model.Db;
using Eng.Agilium.Be.Services;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Roles.List;

public record RoleResult(
  int Id,
  string Title,
  bool IsSystem,
  int SystemKind,
  bool CanViewProject,
  bool CanManageProject,
  bool CanViewMembers,
  bool CanManageMembers,
  bool CanManageSprints,
  bool CanEditItems,
  int MemberCount
);

public record Result(IEnumerable<RoleResult> Roles);

public class Handler(AppDbContext dbContext) : GenericHandler<EmptyCommand, IdParameters, Result>
{
  public override async Task<Result> HandleAsync(
    EmptyCommand command,
    IdParameters parameters,
    CancellationToken cancellationToken
  )
  {
    await Access.EnsureCanAsync(parameters.Id, ProjectPermission.ViewMembers, cancellationToken);

    var roles = await dbContext
      .Roles //
      .AsNoTracking()
      .Where(r => r.ProjectId == parameters.Id)
      .OrderBy(r => r.SystemKind == RoleSystemKind.None ? 1 : 0)
      .ThenBy(r => r.SystemKind)
      .ThenBy(r => r.Title)
      .Select(r => new RoleResult(
        r.Id,
        r.Title,
        r.SystemKind != RoleSystemKind.None,
        (int)r.SystemKind,
        r.CanViewProject,
        r.CanManageProject,
        r.CanViewMembers,
        r.CanManageMembers,
        r.CanManageSprints,
        r.CanEditItems,
        dbContext.Memberships.Count(m => m.RoleId == r.Id)
      ))
      .ToListAsync(cancellationToken);

    return new Result(roles);
  }
}

[EndpointSummary("Lists the roles of a project")]
public class Endpoint : GenericOkEndpoint<EmptyCommand, IdParameters, Handler, Result>
{
  public override HttpMethod Method => HttpMethod.Get;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "{id}/roles";
  public override EndpointPolicy Policy => EndpointPolicy.Project(ProjectPermission.ViewMembers);
}

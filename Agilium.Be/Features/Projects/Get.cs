using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Model.Db;
using Eng.Agilium.Be.Services;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Projects.Get;

/// <summary>What the caller may do in the project; the UI uses it to show/hide controls (the BE enforces it anyway).</summary>
public record PermissionsResult(
  bool CanViewProject,
  bool CanManageProject,
  bool CanViewMembers,
  bool CanManageMembers,
  bool CanManageSprints,
  bool CanEditItems,
  bool IsOwner
);

public record Result(
  int Id,
  string Title,
  string Description,
  int Status,
  int MemberCount,
  int RoleId,
  string RoleTitle,
  PermissionsResult Permissions
);

public class Handler(AppDbContext dbContext) : GenericHandler<EmptyCommand, IdParameters, Result>
{
  public override async Task<Result> HandleAsync(
    EmptyCommand command,
    IdParameters parameters,
    CancellationToken cancellationToken
  )
  {
    var access = await Access.EnsureCanAsync(parameters.Id, ProjectPermission.ViewProject, cancellationToken);

    var project =
      await dbContext
        .Projects //
        .AsNoTracking()
        .Where(p => p.Id == parameters.Id)
        .Select(p => new
        {
          p.Id,
          p.Title,
          p.Description,
          p.State,
          MemberCount = p.Memberships.Count,
        })
        .FirstOrDefaultAsync(cancellationToken)
      ?? throw new EntityNotFoundException(typeof(Project), parameters.Id);
    var roleTitle = await dbContext.Roles.Where(r => r.Id == access.RoleId).Select(r => r.Title).FirstAsync(cancellationToken);

    return new Result(
      project.Id,
      project.Title,
      project.Description,
      (int)project.State,
      project.MemberCount,
      access.RoleId,
      roleTitle,
      new PermissionsResult(
        access.CanViewProject,
        access.CanManageProject,
        access.CanViewMembers,
        access.CanManageMembers,
        access.CanManageSprints,
        access.CanEditItems,
        access.IsOwner
      )
    );
  }
}

[EndpointSummary("Returns a project with the permissions of the logged-in user in it")]
public class Endpoint : GenericOkEndpoint<EmptyCommand, IdParameters, Handler, Result>
{
  public override HttpMethod Method => HttpMethod.Get;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "{id}";
  public override EndpointPolicy Policy => EndpointPolicy.Project(ProjectPermission.ViewProject);
}

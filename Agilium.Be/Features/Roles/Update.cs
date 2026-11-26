using System.Data;
using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Exceptions.Validation;
using Eng.Agilium.Be.Model.Db;
using Eng.Agilium.Be.Services;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Roles.Update;

public record Command(
  [property: XNonEmpty(XValidationErrorKey.INVALID_TITLE)] string Title,
  bool CanViewProject,
  bool CanManageProject,
  bool CanViewMembers,
  bool CanManageMembers,
  bool CanManageSprints,
  bool CanEditItems
);

public class Handler(AppDbContext dbContext, IAccessCache accessCache) : GenericHandler<Command, IdParameters, EmptyResult>
{
  public override async Task<EmptyResult> HandleAsync(
    Command command,
    IdParameters parameters,
    CancellationToken cancellationToken
  )
  {
    int projectId;
    using (var tx = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken))
    {
      var role =
        await dbContext.Roles.FirstOrDefaultAsync(r => r.Id == parameters.Id, cancellationToken)
        ?? throw new EntityNotFoundException(typeof(Role), parameters.Id);
      projectId = role.ProjectId;
      await Access.EnsureCanAsync(projectId, ProjectPermission.ManageMembers, cancellationToken);

      if (role.SystemKind != RoleSystemKind.None)
        throw new BadRequestException("System roles cannot be changed");

      var title = command.Title.Trim();
      if (title.Length > 128)
        throw new BadRequestException("Role title is too long");
      if (await dbContext.Roles.AnyAsync(r => r.ProjectId == projectId && r.Title == title && r.Id != role.Id, cancellationToken))
        throw new EntityAlreadyExistsException(typeof(Role), title);

      role.Title = title;
      role.CanViewProject = command.CanViewProject;
      role.CanManageProject = command.CanManageProject;
      role.CanViewMembers = command.CanViewMembers;
      role.CanManageMembers = command.CanManageMembers;
      role.CanManageSprints = command.CanManageSprints;
      role.CanEditItems = command.CanEditItems;
      await dbContext.SaveChangesAsync(cancellationToken);
      await MembershipRules.EnsureProjectStaysManageableAsync(dbContext, projectId, cancellationToken);
      await tx.CommitAsync(cancellationToken);
    }

    accessCache.InvalidateProject(projectId);
    return new EmptyResult();
  }
}

[EndpointSummary("Updates a custom role (system roles are read-only)")]
public class Endpoint : GenericOkEndpoint<Command, IdParameters, Handler, EmptyResult>
{
  public override HttpMethod Method => HttpMethod.Patch;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "roles/{id}";
  public override EndpointPolicy Policy => EndpointPolicy.Project(ProjectPermission.ManageMembers);
}

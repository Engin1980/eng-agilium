using System.Data;
using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Model.Db;
using Eng.Agilium.Be.Services;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Projects.UpdateMemberRole;

public record Command(int RoleId);

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
      var membership =
        await dbContext.Memberships.Include(m => m.Role).FirstOrDefaultAsync(m => m.Id == parameters.Id, cancellationToken)
        ?? throw new EntityNotFoundException(typeof(Membership), parameters.Id);
      projectId = membership.ProjectId;

      var access = await Access.EnsureCanAsync(projectId, ProjectPermission.ManageMembers, cancellationToken);

      var role =
        await dbContext.Roles.FirstOrDefaultAsync(r => r.Id == command.RoleId, cancellationToken)
        ?? throw new EntityNotFoundException(typeof(Role), command.RoleId);
      if (role.ProjectId != projectId)
        throw new BadRequestException("Role does not belong to the project");

      // only an owner may appoint an owner or change/demote one
      if ((role.SystemKind == RoleSystemKind.Owner || membership.Role.SystemKind == RoleSystemKind.Owner) && !access.IsOwner)
        throw new AuthorizationFailedException();

      membership.RoleId = role.Id;
      await dbContext.SaveChangesAsync(cancellationToken);
      await MembershipRules.EnsureProjectStaysManageableAsync(dbContext, projectId, cancellationToken);
      await tx.CommitAsync(cancellationToken);
    }

    accessCache.InvalidateProject(projectId);
    return new EmptyResult();
  }
}

[EndpointSummary("Changes the role of a member")]
public class Endpoint : GenericOkEndpoint<Command, IdParameters, Handler, EmptyResult>
{
  public override HttpMethod Method => HttpMethod.Patch;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "members/{id}";
  public override EndpointPolicy Policy => EndpointPolicy.Project(ProjectPermission.ManageMembers);
}

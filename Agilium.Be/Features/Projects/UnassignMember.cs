using System.Data;
using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Model.Db;
using Eng.Agilium.Be.Services;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Projects.UnassignMember;

public class Handler(AppDbContext dbContext, IAccessCache accessCache)
  : GenericHandler<EmptyCommand, IdParameters, EmptyResult>
{
  public override async Task<EmptyResult> HandleAsync(
    EmptyCommand command,
    IdParameters parameters,
    CancellationToken cancellationToken
  )
  {
    var userId = LoggedUser?.AppUserId ?? throw new InvalidOperationException("Login is required.");
    int projectId;

    using (var tx = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken))
    {
      var membership =
        await dbContext.Memberships.Include(m => m.Role).FirstOrDefaultAsync(m => m.Id == parameters.Id, cancellationToken)
        ?? throw new EntityNotFoundException(typeof(Membership), parameters.Id);
      projectId = membership.ProjectId;

      // A member may always leave the project on their own; removing somebody else needs ManageMembers.
      if (membership.UserId == userId)
        await Access.GetAsync(projectId, cancellationToken);
      else
        await Access.EnsureCanAsync(projectId, ProjectPermission.ManageMembers, cancellationToken);

      if (membership.Role.SystemKind == RoleSystemKind.Owner)
      {
        // only an owner may remove an owner, and the project always keeps at least one
        var access = await Access.GetAsync(projectId, cancellationToken);
        if (!access.IsOwner)
          throw new AuthorizationFailedException();
        var otherOwners = await dbContext.Memberships.CountAsync(
          m => m.ProjectId == projectId && m.Id != membership.Id && m.Role.SystemKind == RoleSystemKind.Owner,
          cancellationToken
        );
        if (otherOwners == 0)
          throw new BadRequestException("The project must keep at least one owner");
      }

      var isAssigned = await dbContext.Items.AnyAsync(
        i => i.ProjectId == membership.ProjectId && i.AssigneeId == membership.UserId,
        cancellationToken
      );

      if (isAssigned)
        throw new BadRequestException("Cannot remove membership: user is assigned to items in the project");

      dbContext.Memberships.Remove(membership);

      await dbContext.SaveChangesAsync(cancellationToken);
      await MembershipRules.EnsureProjectStaysManageableAsync(dbContext, projectId, cancellationToken);

      await tx.CommitAsync(cancellationToken);
    }

    // after the commit, so a concurrent request cannot cache the old state again
    accessCache.InvalidateProject(projectId);

    return new EmptyResult();
  }
}

[EndpointSummary("Removes a membership by id (a member can remove their own membership, i.e. leave the project)")]
public class Endpoint : GenericOkEndpoint<EmptyCommand, IdParameters, Handler, EmptyResult>
{
  public override HttpMethod Method => HttpMethod.Delete;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "membership/{id}";
  public override EndpointPolicy Policy => EndpointPolicy.Project(ProjectPermission.ManageMembers);
}

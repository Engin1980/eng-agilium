using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Model.Db;
using Eng.Agilium.Be.Services;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Roles.Delete;

public class Handler(AppDbContext dbContext) : GenericHandler<EmptyCommand, IdParameters, EmptyResult>
{
  public override async Task<EmptyResult> HandleAsync(
    EmptyCommand command,
    IdParameters parameters,
    CancellationToken cancellationToken
  )
  {
    var role =
      await dbContext.Roles.FirstOrDefaultAsync(r => r.Id == parameters.Id, cancellationToken)
      ?? throw new EntityNotFoundException(typeof(Role), parameters.Id);
    await Access.EnsureCanAsync(role.ProjectId, ProjectPermission.ManageMembers, cancellationToken);

    if (role.SystemKind != RoleSystemKind.None)
      throw new BadRequestException("System roles cannot be deleted");
    if (await dbContext.Memberships.AnyAsync(m => m.RoleId == role.Id, cancellationToken))
      throw new BadRequestException("The role is still assigned to members");

    // invitation keys of the role are of no use any more
    dbContext.ProjectInvitations.RemoveRange(dbContext.ProjectInvitations.Where(i => i.RoleId == role.Id));
    dbContext.Roles.Remove(role);
    await dbContext.SaveChangesAsync(cancellationToken);
    return new EmptyResult();
  }
}

[EndpointSummary("Deletes a custom role that has no members")]
public class Endpoint : GenericOkEndpoint<EmptyCommand, IdParameters, Handler, EmptyResult>
{
  public override HttpMethod Method => HttpMethod.Delete;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "roles/{id}";
  public override EndpointPolicy Policy => EndpointPolicy.Project(ProjectPermission.ManageMembers);
}

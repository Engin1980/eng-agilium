using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Model.Db;
using Eng.Agilium.Be.Services;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Invitations.Revoke;

public class Handler(AppDbContext dbContext, TimeProvider clock) : GenericHandler<EmptyCommand, IdParameters, EmptyResult>
{
  public override async Task<EmptyResult> HandleAsync(
    EmptyCommand command,
    IdParameters parameters,
    CancellationToken cancellationToken
  )
  {
    var invitation =
      await dbContext.ProjectInvitations.FirstOrDefaultAsync(i => i.Id == parameters.Id, cancellationToken)
      ?? throw new EntityNotFoundException(typeof(ProjectInvitation), parameters.Id);
    await Access.EnsureCanAsync(invitation.ProjectId, ProjectPermission.ManageMembers, cancellationToken);

    invitation.RevokedAt ??= clock.GetUtcNow().UtcDateTime;
    await dbContext.SaveChangesAsync(cancellationToken);
    return new EmptyResult();
  }
}

[EndpointSummary("Revokes an invitation key")]
public class Endpoint : GenericOkEndpoint<EmptyCommand, IdParameters, Handler, EmptyResult>
{
  public override HttpMethod Method => HttpMethod.Delete;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "invitations/{id}";
  public override EndpointPolicy Policy => EndpointPolicy.Project(ProjectPermission.ManageMembers);
}

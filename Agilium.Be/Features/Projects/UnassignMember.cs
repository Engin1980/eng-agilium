using System.Data;
using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Model.Db;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Projects.UnassignMember;

public class Handler(AppDbContext dbContext) : GenericHandler<EmptyCommand, IdParameters, EmptyResult>
{
  private readonly AppDbContext dbContext = dbContext;

  public override async Task<EmptyResult> HandleAsync(
    EmptyCommand command,
    IdParameters parameters,
    CancellationToken cancellationToken
  )
  {
    using (var tx = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken))
    {
      var membership =
        await dbContext.Memberships.FirstOrDefaultAsync(m => m.Id == parameters.Id, cancellationToken)
        ?? throw new EntityNotFoundException(typeof(Membership), parameters.Id);

      var isAssigned = await dbContext.Items.AnyAsync(
        i => i.ProjectId == membership.ProjectId && i.AssigneeId == membership.UserId,
        cancellationToken
      );

      if (isAssigned)
        throw new BadRequestException("Cannot remove membership: user is assigned to items in the project");

      dbContext.Memberships.Remove(membership);

      await dbContext.SaveChangesAsync(cancellationToken);

      await tx.CommitAsync(cancellationToken);
    }

    return new EmptyResult();
  }
}

[EndpointSummary("Removes a membership by id")]
public class Endpoint : GenericOkEndpoint<EmptyCommand, IdParameters, Handler, EmptyResult>
{
  public override HttpMethod Method => HttpMethod.Delete;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "membership/{id}";
  public override string[] RequiredRoles => [];
}

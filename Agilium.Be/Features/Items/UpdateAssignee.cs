using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Exceptions.Validation;
using Eng.Agilium.Be.Model.Db;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Items.UpdateAssignee;

public record Command(int? AssigneeId);

public class Handler(AppDbContext dbContext) : GenericHandler<Command, IdParameters, EmptyResult>
{
  private readonly AppDbContext dbContext = dbContext;

  public override async Task<EmptyResult> HandleAsync(
    Command command,
    IdParameters parameters,
    CancellationToken cancellationToken
  )
  {
    var item =
      await dbContext.Items.FirstOrDefaultAsync(i => i.Id == parameters.Id, cancellationToken)
      ?? throw new EntityNotFoundException(typeof(Item), parameters.Id);

    if (command.AssigneeId is int aId)
    {
      var user =
        await dbContext.AppUsers.FirstOrDefaultAsync(u => u.Id == aId, cancellationToken)
        ?? throw new EntityNotFoundException(typeof(AppUser), aId);

      var isMember = await dbContext.Memberships.AnyAsync(
        m => m.UserId == aId && m.ProjectId == item.ProjectId,
        cancellationToken
      );
      if (!isMember)
        throw new BadRequestException("Assignee is not a member of the project");

      item.AssigneeId = aId;
    }
    else
    {
      item.AssigneeId = null;
    }

    await dbContext.SaveChangesAsync(cancellationToken);

    return new EmptyResult();
  }
}

[EndpointSummary("Updates assignee of an item by id")]
public class Endpoint : GenericOkEndpoint<Command, IdParameters, Handler, EmptyResult>
{
  public override HttpMethod Method => HttpMethod.Patch;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "items/{id}/assignee";
  public override string[] RequiredRoles => [];
}

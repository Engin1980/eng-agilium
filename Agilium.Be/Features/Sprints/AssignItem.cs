using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Model.Db;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Sprints.AssignItem;

public record Command(int ItemId);

public class Handler(AppDbContext dbContext) : GenericHandler<Command, IdParameters, EmptyResult>
{
  private readonly AppDbContext dbContext = dbContext;

  public override async Task<EmptyResult> HandleAsync(
    Command command,
    IdParameters parameters,
    CancellationToken cancellationToken
  )
  {
    var sprint =
      await dbContext.Sprints.AsNoTracking().FirstOrDefaultAsync(s => s.Id == parameters.Id, cancellationToken)
      ?? throw new EntityNotFoundException(typeof(Sprint), parameters.Id);

    if (sprint.State == SprintState.Completed)
      throw new BadRequestException("Items cannot be assigned to a completed sprint");

    var item =
      await dbContext.Items.AsNoTracking().FirstOrDefaultAsync(i => i.Id == command.ItemId, cancellationToken)
      ?? throw new EntityNotFoundException(typeof(Item), command.ItemId);

    if (item.ProjectId != sprint.ProjectId)
      throw new BadRequestException("Item does not belong to the same project as the sprint");

    if (item.Type is not (ItemType.Task or ItemType.Bug))
      throw new BadRequestException("Only tasks and bugs can be assigned to a sprint");

    var sprintItem = await dbContext.SprintItems.FirstOrDefaultAsync(si => si.ItemId == item.Id, cancellationToken);

    if (sprintItem is null)
    {
      // A task/bug belongs to exactly one sprint; it starts in the project's first "to do" column.
      var initialStateId =
        await dbContext
          .WorkflowStates.AsNoTracking()
          .Where(w => w.ProjectId == sprint.ProjectId && w.Type == WorkflowStateType.ToDo)
          .OrderBy(w => w.OrderIndex)
          .Select(w => (int?)w.Id)
          .FirstOrDefaultAsync(cancellationToken)
        ?? throw new BadRequestException("Project has no workflow state of type ToDo");

      await dbContext.SprintItems.AddAsync(
        new SprintItem
        {
          ItemId = item.Id,
          SprintId = sprint.Id,
          WorkflowStateId = initialStateId,
        },
        cancellationToken
      );
    }
    else
    {
      // Already in a sprint: moving to another one leaves the original (keeps the current state).
      sprintItem.SprintId = sprint.Id;
    }

    await dbContext.SaveChangesAsync(cancellationToken);

    return new EmptyResult();
  }
}

[EndpointSummary("Assigns a task/bug to a sprint (moves it from its previous sprint, if any)")]
public class Endpoint : GenericOkEndpoint<Command, IdParameters, Handler, EmptyResult>
{
  public override HttpMethod Method => HttpMethod.Post;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "sprints/{id}/items";
  public override string[] RequiredRoles => [];
}

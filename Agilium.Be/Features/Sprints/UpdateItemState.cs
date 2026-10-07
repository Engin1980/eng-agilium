using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Model.Db;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Sprints.UpdateItemState;

public record Command(int WorkflowStateId);

public record Parameters(int Id, int ItemId);

public class Handler(AppDbContext dbContext) : GenericHandler<Command, Parameters, EmptyResult>
{
  private readonly AppDbContext dbContext = dbContext;

  public override async Task<EmptyResult> HandleAsync(
    Command command,
    Parameters parameters,
    CancellationToken cancellationToken
  )
  {
    var sprintItem =
      await dbContext
        .SprintItems //
        .Include(si => si.Sprint)
        .FirstOrDefaultAsync(si => si.SprintId == parameters.Id && si.ItemId == parameters.ItemId, cancellationToken)
      ?? throw new EntityNotFoundException(typeof(SprintItem), parameters.ItemId);

    var stateExists = await dbContext.WorkflowStates.AnyAsync(
      w => w.Id == command.WorkflowStateId && w.ProjectId == sprintItem.Sprint.ProjectId,
      cancellationToken
    );
    if (!stateExists)
      throw new EntityNotFoundException(typeof(WorkflowState), command.WorkflowStateId);

    // Free move: any state of the project is allowed, there are no transition restrictions.
    sprintItem.WorkflowStateId = command.WorkflowStateId;
    await dbContext.SaveChangesAsync(cancellationToken);

    return new EmptyResult();
  }
}

[EndpointSummary("Moves a sprint item to another workflow state (kanban column)")]
public class Endpoint : GenericOkEndpoint<Command, Parameters, Handler, EmptyResult>
{
  public override HttpMethod Method => HttpMethod.Patch;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "sprints/{id}/items/{itemId}/state";
  public override string[] RequiredRoles => [];
}

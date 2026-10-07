using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Model.Db;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Workflow.Update;

public record StateInput(int? Id, string Title, WorkflowStateType Type);

public record Command(List<StateInput> States);

public record Result(int MovedItems);

public class Handler(AppDbContext dbContext) : GenericHandler<Command, IdParameters, Result>
{
  private const int MaxTitleLength = 256;

  private readonly AppDbContext dbContext = dbContext;

  public override async Task<Result> HandleAsync(
    Command command,
    IdParameters parameters,
    CancellationToken cancellationToken
  )
  {
    await dbContext.Projects.EnsureExistsAsync(parameters.Id, cancellationToken);
    Validate(command.States);

    using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);

    var existing = await dbContext
      .WorkflowStates.Where(w => w.ProjectId == parameters.Id)
      .OrderBy(w => w.OrderIndex)
      .ToListAsync(cancellationToken);
    var existingById = existing.ToDictionary(w => w.Id);

    foreach (var input in command.States.Where(s => s.Id is not null))
    {
      if (!existingById.ContainsKey(input.Id!.Value))
        throw new BadRequestException($"Workflow state {input.Id} does not belong to this project");
    }

    var keptIds = command.States.Where(s => s.Id is not null).Select(s => s.Id!.Value).ToHashSet();
    var removed = existing.Where(w => !keptIds.Contains(w.Id)).ToList();

    // Update kept states and create new ones, in the requested order (OrderIndex = 1..n).
    var saved = new List<WorkflowState>();
    for (var i = 0; i < command.States.Count; i++)
    {
      var input = command.States[i];
      WorkflowState state;
      if (input.Id is int id)
      {
        state = existingById[id];
      }
      else
      {
        state = new WorkflowState { ProjectId = parameters.Id };
        await dbContext.WorkflowStates.AddAsync(state, cancellationToken);
      }
      state.Title = input.Title.Trim();
      state.Type = input.Type;
      state.OrderIndex = i + 1;
      saved.Add(state);
    }
    await dbContext.SaveChangesAsync(cancellationToken);

    // Items of a removed column move to the nearest kept column on its left (by the original order);
    // when there is none (the first column was removed), to the nearest kept column on its right.
    var moved = 0;
    if (removed.Count > 0)
    {
      var removedIds = removed.Select(w => w.Id).ToList();
      var sprintItems = await dbContext
        .SprintItems.Where(si => removedIds.Contains(si.WorkflowStateId))
        .ToListAsync(cancellationToken);

      var targetByRemovedId = removed.ToDictionary(w => w.Id, w => FindTarget(w, existing, keptIds)?.Id ?? saved[0].Id);
      foreach (var sprintItem in sprintItems)
        sprintItem.WorkflowStateId = targetByRemovedId[sprintItem.WorkflowStateId];
      moved = sprintItems.Count;
      await dbContext.SaveChangesAsync(cancellationToken);

      dbContext.WorkflowStates.RemoveRange(removed);
      await dbContext.SaveChangesAsync(cancellationToken);
    }

    await tx.CommitAsync(cancellationToken);

    return new Result(moved);
  }

  private static WorkflowState? FindTarget(WorkflowState removed, List<WorkflowState> original, HashSet<int> keptIds)
  {
    var index = original.IndexOf(removed);
    for (var i = index - 1; i >= 0; i--)
    {
      if (keptIds.Contains(original[i].Id))
        return original[i];
    }
    for (var i = index + 1; i < original.Count; i++)
    {
      if (keptIds.Contains(original[i].Id))
        return original[i];
    }

    // No old column survives (every column is new): the caller falls back to the first new column.
    return null;
  }

  private static void Validate(List<StateInput> states)
  {
    if (states is null || states.Count < 2)
      throw new BadRequestException("Workflow needs at least 2 states");

    if (!states.Any(s => s.Type == WorkflowStateType.ToDo))
      throw new BadRequestException("Workflow needs at least one state of type ToDo");

    if (!states.Any(s => s.Type == WorkflowStateType.Done))
      throw new BadRequestException("Workflow needs at least one state of type Done");

    foreach (var state in states)
    {
      if (string.IsNullOrWhiteSpace(state.Title) || state.Title.Trim().Length > MaxTitleLength)
        throw new BadRequestException($"Workflow state title must be 1-{MaxTitleLength} characters");
      if (!Enum.IsDefined(state.Type))
        throw new BadRequestException("Invalid workflow state type");
    }

    var ids = states.Where(s => s.Id is not null).Select(s => s.Id!.Value).ToList();
    if (ids.Distinct().Count() != ids.Count)
      throw new BadRequestException("Workflow state ids must not repeat");
  }
}

[EndpointSummary("Replaces the workflow states (kanban columns) of a project; items of removed states move left")]
public class Endpoint : GenericOkEndpoint<Command, IdParameters, Handler, Result>
{
  public override HttpMethod Method => HttpMethod.Put;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "{id}/workflow";
  public override string[] RequiredRoles => [];
}

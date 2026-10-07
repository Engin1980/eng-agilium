using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Model.Db;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Sprints.GetBoard;

public record SprintResult(int Id, string Title, DateTime? StartDateTime, DateTime? EndDateTime, int State);

public record CardResult(
  int ItemId,
  string Title,
  int Type,
  string? AssigneeName,
  int? UserStoryId,
  string? UserStoryTitle,
  int? FeatureId,
  string? FeatureTitle
);

public record ColumnResult(int Id, string Title, int Type, List<CardResult> Cards);

/// <summary>Feature / User-Story present in the sprint, with a status derived from its children in the sprint.</summary>
public record ParentResult(int ItemId, string Title, int Type, bool IsGeneric, int Status, List<ParentResult> Children);

public record Result(SprintResult Sprint, List<ColumnResult> Columns, List<ParentResult> Features);

public class Handler(AppDbContext dbContext) : GenericHandler<EmptyCommand, IdParameters, Result>
{
  private readonly AppDbContext dbContext = dbContext;

  public override async Task<Result> HandleAsync(
    EmptyCommand command,
    IdParameters parameters,
    CancellationToken cancellationToken
  )
  {
    var sprint =
      await dbContext.Sprints.AsNoTracking().FirstOrDefaultAsync(s => s.Id == parameters.Id, cancellationToken)
      ?? throw new EntityNotFoundException(typeof(Sprint), parameters.Id);

    var states = await dbContext
      .WorkflowStates //
      .AsNoTracking()
      .Where(w => w.ProjectId == sprint.ProjectId)
      .OrderBy(w => w.OrderIndex)
      .ToListAsync(cancellationToken);
    var stateTypeById = states.ToDictionary(s => s.Id, s => s.Type);

    var sprintItems = await dbContext
      .SprintItems //
      .AsNoTracking()
      .Where(si => si.SprintId == sprint.Id)
      .ToListAsync(cancellationToken);

    var itemsById = await dbContext
      .Items //
      .AsNoTracking()
      .Include(i => i.Assignee)
      .Where(i => i.ProjectId == sprint.ProjectId)
      .ToDictionaryAsync(i => i.Id, cancellationToken);

    var columns = states
      .Select(state => new ColumnResult(
        state.Id,
        state.Title,
        (int)state.Type,
        sprintItems
          .Where(si => si.WorkflowStateId == state.Id)
          .Select(si => ToCard(itemsById[si.ItemId], itemsById))
          .OrderBy(c => c.Title, StringComparer.Ordinal)
          .ToList()
      ))
      .ToList();

    return new Result(
      new SprintResult(sprint.Id, sprint.Title, sprint.StartDateTime, sprint.EndDateTime, (int)sprint.State),
      columns,
      BuildParents(sprintItems, stateTypeById, itemsById)
    );
  }

  private static CardResult ToCard(Item item, Dictionary<int, Item> itemsById)
  {
    var userStory = item.ParentId is int usId ? itemsById.GetValueOrDefault(usId) : null;
    var feature = userStory?.ParentId is int fId ? itemsById.GetValueOrDefault(fId) : null;

    return new CardResult(
      item.Id,
      item.Title,
      (int)item.Type,
      item.Assignee is null ? null : $"{item.Assignee.Name} {item.Assignee.Surname}".Trim(),
      userStory?.Id,
      userStory?.Title,
      feature?.Id,
      feature?.Title
    );
  }

  /// <summary>
  /// A Feature / User-Story appears in the sprint when at least one of its descendant tasks/bugs is in it;
  /// its status is derived (see <see cref="SprintRules.DeriveStatus"/>) from the children present in the sprint.
  /// </summary>
  private static List<ParentResult> BuildParents(
    List<SprintItem> sprintItems,
    Dictionary<int, WorkflowStateType> stateTypeById,
    Dictionary<int, Item> itemsById
  )
  {
    var storyChildren = sprintItems
      .Select(si => (Item: itemsById[si.ItemId], Status: stateTypeById[si.WorkflowStateId]))
      .Where(x => x.Item.ParentId is not null)
      .GroupBy(x => x.Item.ParentId!.Value)
      .ToList();

    var stories = storyChildren
      .Select(g => (Story: itemsById[g.Key], Status: SprintRules.DeriveStatus([.. g.Select(x => x.Status)])))
      .ToList();

    var features = stories
      .GroupBy(s => s.Story.ParentId)
      .Select(g =>
      {
        var children = g.OrderBy(s => s.Story.Title, StringComparer.Ordinal)
          .Select(s => new ParentResult(s.Story.Id, s.Story.Title, (int)s.Story.Type, s.Story.IsGeneric, (int)s.Status, []))
          .ToList();
        var status = SprintRules.DeriveStatus([.. g.Select(s => s.Status)]);

        // A story without a feature parent cannot occur (Items enforce the hierarchy); guard anyway.
        var feature = g.Key is int fId ? itemsById.GetValueOrDefault(fId) : null;
        return feature is null
          ? null
          : new ParentResult(feature.Id, feature.Title, (int)feature.Type, feature.IsGeneric, (int)status, children);
      })
      .OfType<ParentResult>()
      .OrderBy(f => f.Title, StringComparer.Ordinal)
      .ToList();

    return features;
  }
}

[EndpointSummary("Returns the kanban board of a sprint: columns with task/bug cards and the derived feature/user-story overview")]
public class Endpoint : GenericOkEndpoint<EmptyCommand, IdParameters, Handler, Result>
{
  public override HttpMethod Method => HttpMethod.Get;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "sprints/{id}/board";
  public override string[] RequiredRoles => [];
}

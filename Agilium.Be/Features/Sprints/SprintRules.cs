using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Model.Db;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Sprints;

/// <summary>Validation and derivation rules shared by the sprint features.</summary>
public static class SprintRules
{
  public static void EnsureValidDates(DateTime? start, DateTime? end)
  {
    if (start is DateTime s && end is DateTime e && e < s)
      throw new BadRequestException("Sprint end cannot be before its start");
  }

  public static async Task EnsureUniqueTitleAsync(
    AppDbContext dbContext,
    int projectId,
    string title,
    int? exceptSprintId,
    CancellationToken cancellationToken
  )
  {
    var exists = await dbContext.Sprints.AnyAsync(
      s => s.ProjectId == projectId && s.Title == title && s.Id != exceptSprintId,
      cancellationToken
    );
    if (exists)
      throw new EntityAlreadyExistsException(typeof(Sprint), title);
  }

  /// <summary>
  /// Status of a Feature / User-Story within one sprint, derived from the statuses of its children that
  /// are in that sprint: all ToDo -> ToDo, all Done -> Done, anything else -> Active.
  /// </summary>
  public static WorkflowStateType DeriveStatus(IReadOnlyCollection<WorkflowStateType> childStatuses)
  {
    if (childStatuses.Count == 0 || childStatuses.All(s => s == WorkflowStateType.ToDo))
      return WorkflowStateType.ToDo;
    if (childStatuses.All(s => s == WorkflowStateType.Done))
      return WorkflowStateType.Done;
    return WorkflowStateType.Active;
  }
}

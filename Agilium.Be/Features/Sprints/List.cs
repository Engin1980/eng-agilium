using Eng.Agilium.Be.Model.Db;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Sprints.List;

public record SprintResult(
  int Id,
  string Title,
  DateTime? StartDateTime,
  DateTime? EndDateTime,
  int State,
  int ItemCount,
  int DoneCount
);

public record Result(List<SprintResult> Sprints);

public class Handler(AppDbContext dbContext) : GenericHandler<EmptyCommand, IdParameters, Result>
{
  private readonly AppDbContext dbContext = dbContext;

  public override async Task<Result> HandleAsync(
    EmptyCommand command,
    IdParameters parameters,
    CancellationToken cancellationToken
  )
  {
    await dbContext.Projects.EnsureExistsAsync(parameters.Id, cancellationToken);

    var sprints = await dbContext
      .Sprints //
      .AsNoTracking()
      .Where(s => s.ProjectId == parameters.Id)
      .OrderBy(s => s.StartDateTime == null)
      .ThenBy(s => s.StartDateTime)
      .ThenBy(s => s.Id)
      .Select(s => new SprintResult(
        s.Id,
        s.Title,
        s.StartDateTime,
        s.EndDateTime,
        (int)s.State,
        dbContext.SprintItems.Count(si => si.SprintId == s.Id),
        dbContext.SprintItems.Count(si => si.SprintId == s.Id && si.WorkflowState.Type == WorkflowStateType.Done)
      ))
      .ToListAsync(cancellationToken);

    return new Result(sprints);
  }
}

[EndpointSummary("Returns sprints of a project")]
public class Endpoint : GenericOkEndpoint<EmptyCommand, IdParameters, Handler, Result>
{
  public override HttpMethod Method => HttpMethod.Get;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "{id}/sprints";
  public override string[] RequiredRoles => [];
}

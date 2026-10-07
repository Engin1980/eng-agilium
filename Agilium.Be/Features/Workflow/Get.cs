using Eng.Agilium.Be.Model.Db;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Workflow.Get;

public record StateResult(int Id, string Title, int Type, int ItemCount);

public record Result(List<StateResult> States);

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

    var states = await dbContext
      .WorkflowStates //
      .AsNoTracking()
      .Where(w => w.ProjectId == parameters.Id)
      .OrderBy(w => w.OrderIndex)
      .Select(w => new StateResult(
        w.Id,
        w.Title,
        (int)w.Type,
        dbContext.SprintItems.Count(si => si.WorkflowStateId == w.Id)
      ))
      .ToListAsync(cancellationToken);

    return new Result(states);
  }
}

[EndpointSummary("Returns the workflow states (kanban columns) of a project in order")]
public class Endpoint : GenericOkEndpoint<EmptyCommand, IdParameters, Handler, Result>
{
  public override HttpMethod Method => HttpMethod.Get;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "{id}/workflow";
  public override string[] RequiredRoles => [];
}

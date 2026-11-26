using Eng.Agilium.Be.Services;
using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Model.Db;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Sprints.UnassignItem;

public record Parameters(int Id, int ItemId);

public class Handler(AppDbContext dbContext) : GenericHandler<EmptyCommand, Parameters, EmptyResult>
{
  private readonly AppDbContext dbContext = dbContext;

  public override async Task<EmptyResult> HandleAsync(
    EmptyCommand command,
    Parameters parameters,
    CancellationToken cancellationToken
  )
  {
    var sprint =
      await dbContext.Sprints.AsNoTracking().FirstOrDefaultAsync(s => s.Id == parameters.Id, cancellationToken)
      ?? throw new EntityNotFoundException(typeof(Sprint), parameters.Id);

    await Access.EnsureCanAsync(sprint.ProjectId, ProjectPermission.EditItems, cancellationToken);

    var sprintItem =
      await dbContext.SprintItems.FirstOrDefaultAsync(
        si => si.SprintId == parameters.Id && si.ItemId == parameters.ItemId,
        cancellationToken
      ) ?? throw new EntityNotFoundException(typeof(SprintItem), parameters.ItemId);

    dbContext.SprintItems.Remove(sprintItem);
    await dbContext.SaveChangesAsync(cancellationToken);

    return new EmptyResult();
  }
}

[EndpointSummary("Removes a task/bug from a sprint")]
public class Endpoint : GenericOkEndpoint<EmptyCommand, Parameters, Handler, EmptyResult>
{
  public override HttpMethod Method => HttpMethod.Delete;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "sprints/{id}/items/{itemId}";
  public override EndpointPolicy Policy => EndpointPolicy.Project(ProjectPermission.EditItems);
}

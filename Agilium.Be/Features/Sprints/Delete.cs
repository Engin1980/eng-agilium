using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Model.Db;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Sprints.Delete;

public class Handler(AppDbContext dbContext) : GenericHandler<EmptyCommand, IdParameters, EmptyResult>
{
  private readonly AppDbContext dbContext = dbContext;

  public override async Task<EmptyResult> HandleAsync(
    EmptyCommand command,
    IdParameters parameters,
    CancellationToken cancellationToken
  )
  {
    var sprint =
      await dbContext.Sprints.FirstOrDefaultAsync(s => s.Id == parameters.Id, cancellationToken)
      ?? throw new EntityNotFoundException(typeof(Sprint), parameters.Id);

    if (await dbContext.SprintItems.AnyAsync(si => si.SprintId == sprint.Id, cancellationToken))
      throw new BadRequestException("Sprint still has assigned items - unassign them first");

    dbContext.Sprints.Remove(sprint);
    await dbContext.SaveChangesAsync(cancellationToken);

    return new EmptyResult();
  }
}

[EndpointSummary("Deletes an empty sprint by id")]
public class Endpoint : GenericOkEndpoint<EmptyCommand, IdParameters, Handler, EmptyResult>
{
  public override HttpMethod Method => HttpMethod.Delete;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "sprints/{id}";
  public override string[] RequiredRoles => [];
}

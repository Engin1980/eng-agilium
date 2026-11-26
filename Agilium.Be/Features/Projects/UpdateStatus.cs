using Eng.Agilium.Be.Services;
using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Exceptions.Validation;
using Eng.Agilium.Be.Model.Db;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Projects.UpdateState;

public record Command([property: XEnumValidation] ProjectState Status);

public class Handler(AppDbContext dbContext) : GenericHandler<Command, IdParameters, EmptyResult>
{
  public override async Task<EmptyResult> HandleAsync(
    Command command,
    IdParameters parameters,
    CancellationToken cancellationToken
  )
  {
    await Access.EnsureCanAsync(parameters.Id, ProjectPermission.ManageProject, cancellationToken);

    var project =
      await dbContext.Projects.FirstOrDefaultAsync(p => p.Id == parameters.Id, cancellationToken)
      ?? throw new EntityNotFoundException(typeof(Project), parameters.Id);

    project.State = command.Status;

    await dbContext.SaveChangesAsync(cancellationToken);

    return new EmptyResult();
  }
}

[EndpointSummary("Updates project state by id")]
public class Endpoint : GenericOkEndpoint<Command, IdParameters, Handler, EmptyResult>
{
  public override HttpMethod Method => HttpMethod.Patch;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "{id}/state";
  public override EndpointPolicy Policy => EndpointPolicy.Project(ProjectPermission.ManageProject);
}

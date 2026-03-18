using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Exceptions.Validation;
using Eng.Agilium.Be.Model.Db;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Projects.Update;

public record Command(
  [property: XNonEmpty(XValidationErrorKey.INVALID_TITLE)] string Title,
  [property: XNotNull(XValidationErrorKey.NULL_DESCRIPTION)] string Description
);

public class Handler(AppDbContext dbContext) : GenericHandler<Command, IdParameters, EmptyResult>
{
  private readonly AppDbContext dbContext = dbContext;

  public override async Task<EmptyResult> HandleAsync(
    Command command,
    IdParameters parameters,
    CancellationToken cancellationToken
  )
  {
    if (dbContext.Projects.Any(p => p.Title == command.Title && p.Id != parameters.Id))
      throw new EntityAlreadyExistsException(typeof(Project), command.Title);

    var project =
      await dbContext.Projects.FirstOrDefaultAsync(p => p.Id == parameters.Id, cancellationToken)
      ?? throw new EntityNotFoundException(typeof(Project), parameters.Id);

    project.Title = command.Title;
    project.Description = command.Description;

    await dbContext.SaveChangesAsync(cancellationToken);

    return new EmptyResult();
  }
}

[EndpointSummary("Updates an existing project by id")]
public class Endpoint : GenericOkEndpoint<Command, IdParameters, Handler, EmptyResult>
{
  public override HttpMethod Method => HttpMethod.Patch;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "{id}";
  public override string[] RequiredRoles => [];
}

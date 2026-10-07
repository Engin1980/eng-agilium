using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Exceptions.Validation;
using Eng.Agilium.Be.Model.Db;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Sprints.Update;

public record Command(
  [property: XNonEmpty(XValidationErrorKey.INVALID_TITLE)] string Title,
  DateTime? StartDateTime,
  DateTime? EndDateTime,
  [property: XEnumValidation] SprintState State
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
    var sprint =
      await dbContext.Sprints.FirstOrDefaultAsync(s => s.Id == parameters.Id, cancellationToken)
      ?? throw new EntityNotFoundException(typeof(Sprint), parameters.Id);

    var title = command.Title.Trim();
    SprintRules.EnsureValidDates(command.StartDateTime, command.EndDateTime);
    await SprintRules.EnsureUniqueTitleAsync(dbContext, sprint.ProjectId, title, sprint.Id, cancellationToken);

    sprint.Title = title;
    sprint.StartDateTime = command.StartDateTime;
    sprint.EndDateTime = command.EndDateTime;
    sprint.State = command.State;

    await dbContext.SaveChangesAsync(cancellationToken);

    return new EmptyResult();
  }
}

[EndpointSummary("Updates a sprint (title, dates and state) by id")]
public class Endpoint : GenericOkEndpoint<Command, IdParameters, Handler, EmptyResult>
{
  public override HttpMethod Method => HttpMethod.Patch;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "sprints/{id}";
  public override string[] RequiredRoles => [];
}

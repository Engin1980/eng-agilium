using Eng.Agilium.Be.Exceptions.Validation;
using Eng.Agilium.Be.Model.Db;

namespace Eng.Agilium.Be.Features.Sprints.Create;

public record Command(
  int ProjectId,
  [property: XNonEmpty(XValidationErrorKey.INVALID_TITLE)] string Title,
  DateTime? StartDateTime,
  DateTime? EndDateTime
);

public class Handler(AppDbContext dbContext) : GenericHandler<Command, EmptyParameters, IdResult>
{
  private readonly AppDbContext dbContext = dbContext;

  public override async Task<IdResult> HandleAsync(
    Command command,
    EmptyParameters parameters,
    CancellationToken cancellationToken
  )
  {
    await dbContext.Projects.EnsureExistsAsync(command.ProjectId, cancellationToken);

    var title = command.Title.Trim();
    SprintRules.EnsureValidDates(command.StartDateTime, command.EndDateTime);
    await SprintRules.EnsureUniqueTitleAsync(dbContext, command.ProjectId, title, null, cancellationToken);

    var sprint = new Sprint
    {
      Title = title,
      ProjectId = command.ProjectId,
      StartDateTime = command.StartDateTime,
      EndDateTime = command.EndDateTime,
      State = SprintState.Planned,
    };

    await dbContext.Sprints.AddAsync(sprint, cancellationToken);
    await dbContext.SaveChangesAsync(cancellationToken);

    return new IdResult(sprint.Id);
  }
}

[EndpointSummary("Creates a new planned sprint in a project")]
public class Endpoint : GenericCreatedEndpoint<Command, EmptyParameters, Handler, IdResult>
{
  public override HttpMethod Method => HttpMethod.Post;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "sprints";
  public override string[] RequiredRoles => [];
}

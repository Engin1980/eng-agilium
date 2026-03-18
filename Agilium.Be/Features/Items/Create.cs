using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Exceptions.Validation;
using Eng.Agilium.Be.Model.Db;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Items.Create;

public record Command(
  [property: XNonEmpty(XValidationErrorKey.INVALID_TITLE)] string Title,
  [property: XEnumValidation] ItemType Type,
  int ProjectId,
  int? ParentId
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
    var project =
      await dbContext.Projects.FirstOrDefaultAsync(p => p.Id == command.ProjectId, cancellationToken)
      ?? throw new EntityNotFoundException(typeof(Project), command.ProjectId);

    if (command.ParentId is int parentId)
    {
      var parent =
        await dbContext.Items.FirstOrDefaultAsync(i => i.Id == parentId, cancellationToken)
        ?? throw new EntityNotFoundException(typeof(Item), parentId);

      if (parent.ProjectId != project.Id)
        throw new BadRequestException("Parent item does not belong to the same project");
    }

    var item = new Item
    {
      Title = command.Title,
      Type = command.Type,
      ProjectId = command.ProjectId,
      ParentId = command.ParentId,
    };

    await dbContext.Items.AddAsync(item, cancellationToken);
    await dbContext.SaveChangesAsync(cancellationToken);

    return new IdResult(item.Id);
  }
}

[EndpointSummary("Creates a new item")]
public class Endpoint : GenericCreatedEndpoint<Command, EmptyParameters, Handler, IdResult>
{
  public override HttpMethod Method => HttpMethod.Post;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "items";
  public override string[] RequiredRoles => [];
}

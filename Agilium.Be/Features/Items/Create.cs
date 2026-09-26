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

    using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);

    int? parentId = command.ParentId;

    if (parentId is int explicitParentId)
    {
      var parent =
        await dbContext.Items.FirstOrDefaultAsync(i => i.Id == explicitParentId, cancellationToken)
        ?? throw new EntityNotFoundException(typeof(Item), explicitParentId);

      if (parent.ProjectId != project.Id)
        throw new BadRequestException("Parent item does not belong to the same project");

      EnsureValidParentType(command.Type, parent.Type);
    }
    else
    {
      // Feature has no generic ancestor - it simply stays at the root of the tree.
      parentId = command.Type switch
      {
        ItemType.UserStory => await EnsureGenericItemAsync(project.Id, ItemType.Feature, "Bez feature", cancellationToken),
        ItemType.Task or ItemType.Bug => await EnsureGenericItemAsync(
          project.Id,
          ItemType.UserStory,
          "Bez user-story",
          cancellationToken
        ),
        _ => null,
      };
    }

    var item = new Item
    {
      Title = command.Title,
      Type = command.Type,
      ProjectId = command.ProjectId,
      ParentId = parentId,
    };

    await dbContext.Items.AddAsync(item, cancellationToken);
    await dbContext.SaveChangesAsync(cancellationToken);

    await tx.CommitAsync(cancellationToken);

    return new IdResult(item.Id);
  }

  /// <summary>
  /// Enforces the Feature -> User-Story -> Task/Bug hierarchy from description.md: a Feature is always
  /// a root item, a User-Story's parent must be a Feature, and a Task/Bug's parent must be a User-Story.
  /// </summary>
  private static void EnsureValidParentType(ItemType childType, ItemType parentType)
  {
    var isValid = childType switch
    {
      ItemType.Feature => false, // Features never have a parent.
      ItemType.UserStory => parentType == ItemType.Feature,
      ItemType.Task or ItemType.Bug => parentType == ItemType.UserStory,
      _ => false,
    };

    if (!isValid)
      throw new BadRequestException($"A {childType} cannot have a parent of type {parentType}");
  }

  /// <summary>
  /// Finds the project's single generic container item of the given type, lazily creating it (and, for
  /// a generic user-story, its own generic-feature parent) the first time it is needed.
  /// </summary>
  private async Task<int> EnsureGenericItemAsync(
    int projectId,
    ItemType type,
    string title,
    CancellationToken cancellationToken
  )
  {
    var existing = await dbContext.Items.FirstOrDefaultAsync(
      i => i.ProjectId == projectId && i.Type == type && i.IsGeneric,
      cancellationToken
    );
    if (existing != null)
      return existing.Id;

    int? parentId =
      type == ItemType.UserStory
        ? await EnsureGenericItemAsync(projectId, ItemType.Feature, "Bez feature", cancellationToken)
        : null;

    var generic = new Item
    {
      Title = title,
      Type = type,
      ProjectId = projectId,
      ParentId = parentId,
      IsGeneric = true,
    };
    await dbContext.Items.AddAsync(generic, cancellationToken);
    await dbContext.SaveChangesAsync(cancellationToken);

    return generic.Id;
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

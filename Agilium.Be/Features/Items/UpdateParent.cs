using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Model.Db;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Items.UpdateParent;

public record Command(int? ParentId);

public class Handler(AppDbContext dbContext) : GenericHandler<Command, IdParameters, EmptyResult>
{
  private readonly AppDbContext dbContext = dbContext;

  public override async Task<EmptyResult> HandleAsync(
    Command command,
    IdParameters parameters,
    CancellationToken cancellationToken
  )
  {
    var item =
      await dbContext.Items.FirstOrDefaultAsync(i => i.Id == parameters.Id, cancellationToken)
      ?? throw new EntityNotFoundException(typeof(Item), parameters.Id);

    if (item.IsGeneric)
      throw new BadRequestException("Generic container items cannot be moved");

    if (command.ParentId is int pId)
    {
      var parent =
        await dbContext.Items.FirstOrDefaultAsync(i => i.Id == pId, cancellationToken)
        ?? throw new EntityNotFoundException(typeof(Item), pId);

      if (parent.ProjectId != item.ProjectId)
        throw new BadRequestException("Parent item does not belong to the same project");

      EnsureValidParentType(item.Type, parent.Type);

      await EnsureNoCycleAsync(item.Id, pId, cancellationToken);

      item.ParentId = pId;
    }
    else
    {
      if (item.Type != ItemType.Feature)
        throw new BadRequestException($"A {item.Type} always requires a parent");

      item.ParentId = null;
    }

    await dbContext.SaveChangesAsync(cancellationToken);

    return new EmptyResult();
  }

  /// <summary>Same Feature -> User-Story -> Task/Bug rule enforced on creation (see Items.Create).</summary>
  private static void EnsureValidParentType(ItemType childType, ItemType parentType)
  {
    var isValid = childType switch
    {
      ItemType.UserStory => parentType == ItemType.Feature,
      ItemType.Task or ItemType.Bug => parentType == ItemType.UserStory,
      _ => false,
    };

    if (!isValid)
      throw new BadRequestException($"A {childType} cannot have a parent of type {parentType}");
  }

  /// <summary>
  /// Walks up from the candidate new parent to the project root, rejecting the move if it would make
  /// `itemId` its own ancestor (a direct self-parent is just the pId == itemId case of this walk).
  /// </summary>
  private async Task EnsureNoCycleAsync(int itemId, int newParentId, CancellationToken cancellationToken)
  {
    var currentId = (int?)newParentId;
    while (currentId is int id)
    {
      if (id == itemId)
        throw new BadRequestException("Cannot move item under one of its own descendants");

      currentId = await dbContext
        .Items.Where(i => i.Id == id)
        .Select(i => i.ParentId)
        .FirstOrDefaultAsync(cancellationToken);
    }
  }
}

[EndpointSummary("Updates parent of an item by id")]
public class Endpoint : GenericOkEndpoint<Command, IdParameters, Handler, EmptyResult>
{
  public override HttpMethod Method => HttpMethod.Patch;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "items/{id}/parent";
  public override string[] RequiredRoles => [];
}

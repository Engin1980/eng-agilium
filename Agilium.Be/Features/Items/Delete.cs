using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Model.Db;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Items.Delete;

public class Handler(AppDbContext dbContext) : GenericHandler<EmptyCommand, IdParameters, EmptyResult>
{
  private readonly AppDbContext dbContext = dbContext;

  public override async Task<EmptyResult> HandleAsync(
    EmptyCommand command,
    IdParameters parameters,
    CancellationToken cancellationToken
  )
  {
    var item =
      await dbContext.Items.FirstOrDefaultAsync(i => i.Id == parameters.Id, cancellationToken)
      ?? throw new EntityNotFoundException(typeof(Item), parameters.Id);

    if (item.IsGeneric)
      throw new BadRequestException("Generic container items cannot be deleted");

    using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);

    var projectItems = await dbContext.Items.Where(i => i.ProjectId == item.ProjectId).ToListAsync(cancellationToken);

    var subtree = CollectSubtreeDeepestFirst(item.Id, projectItems);
    var subtreeIds = subtree.Select(i => i.Id).ToList();

    var sprintItems = await dbContext
      .SprintItems.Where(si => subtreeIds.Contains(si.ItemId))
      .ToListAsync(cancellationToken);
    dbContext.SprintItems.RemoveRange(sprintItems);

    var fieldValues = await dbContext
      .ItemFieldValues.Where(v => subtreeIds.Contains(v.ItemId))
      .ToListAsync(cancellationToken);
    dbContext.ItemFieldValues.RemoveRange(fieldValues);

    dbContext.Items.RemoveRange(subtree);

    await dbContext.SaveChangesAsync(cancellationToken);

    await tx.CommitAsync(cancellationToken);

    return new EmptyResult();
  }

  /// <summary>
  /// Returns `rootId` and all of its descendants, ordered so children always precede their parent -
  /// required because Item.ParentId uses DeleteBehavior.Restrict, so children must be removed first.
  /// </summary>
  private static List<Item> CollectSubtreeDeepestFirst(int rootId, List<Item> allProjectItems)
  {
    var result = new List<Item>();

    void Visit(int id)
    {
      foreach (var child in allProjectItems.Where(i => i.ParentId == id))
        Visit(child.Id);
      result.Add(allProjectItems.First(i => i.Id == id));
    }

    Visit(rootId);
    return result;
  }
}

[EndpointSummary("Deletes an item and its descendants (features/user-stories delete their children)")]
public class Endpoint : GenericOkEndpoint<EmptyCommand, IdParameters, Handler, EmptyResult>
{
  public override HttpMethod Method => HttpMethod.Delete;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "items/{id}";
  public override string[] RequiredRoles => [];
}

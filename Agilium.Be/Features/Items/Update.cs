using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Exceptions.Validation;
using Eng.Agilium.Be.Model.Db;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Items.Update;

public record Command(
  [property: XNonEmpty(XValidationErrorKey.INVALID_TITLE)] string Title,
  [property: XEnumValidation] ItemType Type
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
    var item =
      await dbContext.Items.FirstOrDefaultAsync(i => i.Id == parameters.Id, cancellationToken)
      ?? throw new EntityNotFoundException(typeof(Item), parameters.Id);

    if (item.IsGeneric)
      throw new BadRequestException("Generic container items cannot be renamed or retyped");

    if (item.Type != command.Type)
    {
      // Only a Task<->Bug swap keeps the Feature/User-Story/Task/Bug hierarchy (Story 2) intact, since
      // both sit at the same tier and require the same parent type. Any other change would either
      // invalidate the existing ParentId or make this item nest incorrectly.
      var isTaskBugSwap =
        item.Type is ItemType.Task or ItemType.Bug && command.Type is ItemType.Task or ItemType.Bug;
      if (!isTaskBugSwap)
        throw new BadRequestException($"Cannot change item type from {item.Type} to {command.Type}");

      // Task and Bug each have their own template, so field values from the old type's template
      // reference TemplateItemIds that no longer apply.
      var staleValues = await dbContext
        .ItemFieldValues.Where(v => v.ItemId == item.Id)
        .ToListAsync(cancellationToken);
      dbContext.ItemFieldValues.RemoveRange(staleValues);
    }

    item.Title = command.Title;
    item.Type = command.Type;

    await dbContext.SaveChangesAsync(cancellationToken);

    return new EmptyResult();
  }
}

[EndpointSummary("Updates an existing item by id")]
public class Endpoint : GenericOkEndpoint<Command, IdParameters, Handler, EmptyResult>
{
  public override HttpMethod Method => HttpMethod.Patch;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "items/{id}";
  public override string[] RequiredRoles => [];
}

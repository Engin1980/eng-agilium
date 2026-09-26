using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Exceptions.Validation;
using Eng.Agilium.Be.Model.Db;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Templates.UpdateGlobal;

public record TemplateItemCommand(
  int? Id,
  string Key,
  string Title,
  [property: XEnumValidation] TemplateItemType Type,
  string? ValidatingRegex,
  int OrderIndex,
  int ColumnStart,
  int ColumnSpan,
  int RowStart,
  int RowSpan
);

public record Command(int ColumnCount, List<TemplateItemCommand> Items);

public record Parameters([property: XEnumValidation] ItemType ItemType);

public class Handler(AppDbContext dbContext) : GenericHandler<Command, Parameters, EmptyResult>
{
  public override async Task<EmptyResult> HandleAsync(
    Command command,
    Parameters parameters,
    CancellationToken cancellationToken
  )
  {
    var template =
      await dbContext
        .Templates.Include(t => t.TemplateItems)
        .FirstOrDefaultAsync(t => t.ProjectId == null && t.Type == parameters.ItemType, cancellationToken)
      ?? throw new EntityNotFoundException(typeof(Template), (int)parameters.ItemType);

    var fields = command
      .Items.Select(i => new TemplateFieldInput(
        i.Id,
        i.Key,
        i.Title,
        i.Type,
        i.ValidatingRegex,
        i.OrderIndex,
        i.ColumnStart,
        i.ColumnSpan,
        i.RowStart,
        i.RowSpan
      ))
      .ToList();

    await TemplateGridValidation.ApplyReplaceAsync(dbContext, template, command.ColumnCount, fields, cancellationToken);

    await dbContext.SaveChangesAsync(cancellationToken);

    return new EmptyResult();
  }
}

[EndpointSummary("Replaces the global default template (fields + grid layout) for the given item type")]
[EndpointDescription(
  "Edits the universal default template new projects copy from. Does not retroactively change any "
    + "project's own (already-copied) template."
)]
public class Endpoint : GenericOkEndpoint<Command, Parameters, Handler, EmptyResult>
{
  public override HttpMethod Method => HttpMethod.Put;
  public override BaseRoute BaseRoute => BaseRoute.Templates;
  public override string EndpointRoute => "{itemType}";
  public override string[] RequiredRoles => [];
}

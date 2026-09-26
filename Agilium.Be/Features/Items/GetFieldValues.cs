using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Model.Db;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Items.GetFieldValues;

public record FieldResult(
  int TemplateItemId,
  string Key,
  string Title,
  int Type,
  string? ValidatingRegex,
  int ColumnStart,
  int ColumnSpan,
  int RowStart,
  int RowSpan,
  string? Value
);

public record Result(int ColumnCount, List<FieldResult> Fields);

public class Handler(AppDbContext dbContext) : GenericHandler<EmptyCommand, IdParameters, Result>
{
  public override async Task<Result> HandleAsync(
    EmptyCommand command,
    IdParameters parameters,
    CancellationToken cancellationToken
  )
  {
    var item =
      await dbContext.Items.FirstOrDefaultAsync(i => i.Id == parameters.Id, cancellationToken)
      ?? throw new EntityNotFoundException(typeof(Item), parameters.Id);

    var template =
      await dbContext
        .Templates.AsNoTracking()
        .Include(t => t.TemplateItems)
        .FirstOrDefaultAsync(t => t.ProjectId == item.ProjectId && t.Type == item.Type, cancellationToken)
      ?? throw new EntityNotFoundException(typeof(Template), item.ProjectId);

    var values = await dbContext
      .ItemFieldValues.AsNoTracking()
      .Where(v => v.ItemId == item.Id)
      .ToDictionaryAsync(v => v.TemplateItemId, v => v.Value, cancellationToken);

    var fields = template
      .TemplateItems.OrderBy(ti => ti.OrderIndex)
      .Select(ti => new FieldResult(
        ti.Id,
        ti.Key,
        ti.Title,
        (int)ti.Type,
        ti.ValidatingRegex,
        ti.ColumnStart,
        ti.ColumnSpan,
        ti.RowStart,
        ti.RowSpan,
        values.TryGetValue(ti.Id, out var value) ? value : null
      ))
      .ToList();

    return new Result(template.ColumnCount, fields);
  }
}

[EndpointSummary("Returns an item's template fields merged with its current values")]
public class Endpoint : GenericOkEndpoint<EmptyCommand, IdParameters, Handler, Result>
{
  public override HttpMethod Method => HttpMethod.Get;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "items/{id}/fields";
  public override string[] RequiredRoles => [];
}

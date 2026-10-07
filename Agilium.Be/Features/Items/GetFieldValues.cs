using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Features.Templates;
using Eng.Agilium.Be.Model.Db;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Items.GetFieldValues;

public record AttributeResult(int TemplateItemId, string Key, string Title, int Type, string? ValidatingRegex, string? Value);

public record SectionResult(int Id, string Title, List<AttributeResult> Items);

public record ColumnResult(int Id, int Width, List<SectionResult> Sections);

public record TableResult(int Id, List<ColumnResult> Columns);

public record Result(List<TableResult> Tables);

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
      await dbContext.Templates //
        .AsNoTracking()
        .IncludeTree()
        .FirstOrDefaultAsync(t => t.ProjectId == item.ProjectId && t.Type == item.Type, cancellationToken)
      ?? throw new EntityNotFoundException(typeof(Template), item.ProjectId);

    var values = await dbContext
      .ItemFieldValues.AsNoTracking()
      .Where(v => v.ItemId == item.Id)
      .ToDictionaryAsync(v => v.TemplateItemId, v => v.Value, cancellationToken);

    return new Result(
      template
        .Tables.OrderBy(tb => tb.OrderIndex)
        .Select(tb => new TableResult(
          tb.Id,
          tb
            .Columns.OrderBy(c => c.OrderIndex)
            .Select(c => new ColumnResult(
              c.Id,
              c.Width,
              c.Sections.OrderBy(s => s.OrderIndex)
                .Select(s => new SectionResult(
                  s.Id,
                  s.Title,
                  s.Items.OrderBy(i => i.OrderIndex)
                    .Select(i => new AttributeResult(
                      i.Id,
                      i.Key,
                      i.Title,
                      (int)i.Type,
                      i.ValidatingRegex,
                      values.TryGetValue(i.Id, out var value) ? value : null
                    ))
                    .ToList()
                ))
                .ToList()
            ))
            .ToList()
        ))
        .ToList()
    );
  }
}

[EndpointSummary("Returns an item's template (tables → columns → sections → attributes) merged with its current values")]
public class Endpoint : GenericOkEndpoint<EmptyCommand, IdParameters, Handler, Result>
{
  public override HttpMethod Method => HttpMethod.Get;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "items/{id}/fields";
  public override string[] RequiredRoles => [];
}

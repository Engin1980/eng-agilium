using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Exceptions.Validation;
using Eng.Agilium.Be.Model.Db;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Templates.Get;

public record Parameters(int Id, [property: XEnumValidation] ItemType ItemType);

public record AttributeResult(int Id, string Key, string Title, int Type, string? ValidatingRegex);

public record SectionResult(int Id, string Title, List<AttributeResult> Items);

public record ColumnResult(int Id, int Width, List<SectionResult> Sections);

public record TableResult(int Id, List<ColumnResult> Columns);

public record Result(int TemplateId, List<TableResult> Tables);

public class Handler(AppDbContext dbContext) : GenericHandler<EmptyCommand, Parameters, Result>
{
  public override async Task<Result> HandleAsync(
    EmptyCommand command,
    Parameters parameters,
    CancellationToken cancellationToken
  )
  {
    await dbContext.Projects.EnsureExistsAsync(parameters.Id, cancellationToken);

    var template =
      await dbContext.Templates //
        .AsNoTracking()
        .IncludeTree()
        .FirstOrDefaultAsync(t => t.ProjectId == parameters.Id && t.Type == parameters.ItemType, cancellationToken)
      ?? throw new EntityNotFoundException(typeof(Template), parameters.Id);

    return new Result(
      template.Id,
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
                    .Select(i => new AttributeResult(i.Id, i.Key, i.Title, (int)i.Type, i.ValidatingRegex))
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

[EndpointSummary("Returns a project's template (tables → columns → sections → attributes) for the given item type")]
public class Endpoint : GenericOkEndpoint<EmptyCommand, Parameters, Handler, Result>
{
  public override HttpMethod Method => HttpMethod.Get;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "{id}/templates/{itemType}";
  public override string[] RequiredRoles => [];
}

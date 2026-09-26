using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Exceptions.Validation;
using Eng.Agilium.Be.Model.Db;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Templates.Get;

public record Parameters(int Id, [property: XEnumValidation] ItemType ItemType);

public record TemplateItemResult(
  int Id,
  string Key,
  string Title,
  int Type,
  string? ValidatingRegex,
  int OrderIndex,
  int ColumnStart,
  int ColumnSpan,
  int RowStart,
  int RowSpan
);

public record Result(int TemplateId, int ColumnCount, List<TemplateItemResult> Items);

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
      await dbContext
        .Templates.AsNoTracking()
        .Include(t => t.TemplateItems)
        .FirstOrDefaultAsync(t => t.ProjectId == parameters.Id && t.Type == parameters.ItemType, cancellationToken)
      ?? throw new EntityNotFoundException(typeof(Template), parameters.Id);

    return new Result(
      template.Id,
      template.ColumnCount,
      template
        .TemplateItems.OrderBy(i => i.OrderIndex)
        .Select(i => new TemplateItemResult(
          i.Id,
          i.Key,
          i.Title,
          (int)i.Type,
          i.ValidatingRegex,
          i.OrderIndex,
          i.ColumnStart,
          i.ColumnSpan,
          i.RowStart,
          i.RowSpan
        ))
        .ToList()
    );
  }
}

[EndpointSummary("Returns a project's template (fields + grid layout) for the given item type")]
public class Endpoint : GenericOkEndpoint<EmptyCommand, Parameters, Handler, Result>
{
  public override HttpMethod Method => HttpMethod.Get;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "{id}/templates/{itemType}";
  public override string[] RequiredRoles => [];
}

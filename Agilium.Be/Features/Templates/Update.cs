using System.Text.RegularExpressions;
using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Exceptions.Validation;
using Eng.Agilium.Be.Model.Db;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Templates.Update;

public record AttributeCommand(
  int? Id,
  string Key,
  string Title,
  [property: XEnumValidation] TemplateItemType Type,
  string? ValidatingRegex
);

public record SectionCommand(int? Id, string Title, List<AttributeCommand> Items);

public record ColumnCommand(int? Id, int Width, List<SectionCommand> Sections);

public record TableCommand(int? Id, List<ColumnCommand> Columns);

/// <summary>Full replace of the template tree; the order of the lists is the order (OrderIndex) of the elements.</summary>
public record Command(List<TableCommand> Tables);

public record Parameters(int Id, [property: XEnumValidation] ItemType ItemType);

public record Result(int DeletedFieldValues);

public class Handler(AppDbContext dbContext) : GenericHandler<Command, Parameters, Result>
{
  public override async Task<Result> HandleAsync(
    Command command,
    Parameters parameters,
    CancellationToken cancellationToken
  )
  {
    await dbContext.Projects.EnsureExistsAsync(parameters.Id, cancellationToken);

    var template =
      await dbContext.Templates //
        .IncludeTree()
        .FirstOrDefaultAsync(t => t.ProjectId == parameters.Id && t.Type == parameters.ItemType, cancellationToken)
      ?? throw new EntityNotFoundException(typeof(Template), parameters.Id);

    Validate(command);

    var existingTables = template.Tables.ToDictionary(t => t.Id);
    var existingColumns = template.Tables.SelectMany(t => t.Columns).ToDictionary(c => c.Id);
    var existingSections = existingColumns.Values.SelectMany(c => c.Sections).ToDictionary(s => s.Id);
    var existingItems = existingSections.Values.SelectMany(s => s.Items).ToDictionary(i => i.Id);

    var keptTableIds = new HashSet<int>();
    var keptColumnIds = new HashSet<int>();
    var keptSectionIds = new HashSet<int>();
    var keptItemIds = new HashSet<int>();

    // Existing elements (with an Id) are updated and may move to another parent; elements without an Id
    // are created. Anything not mentioned is removed afterwards.
    for (var ti = 0; ti < command.Tables.Count; ti++)
    {
      var tableCmd = command.Tables[ti];
      var table = Resolve(tableCmd.Id, existingTables, keptTableIds, "table", () => new TemplateTable());
      table.Template = template;
      table.OrderIndex = ti + 1;

      for (var ci = 0; ci < tableCmd.Columns.Count; ci++)
      {
        var columnCmd = tableCmd.Columns[ci];
        var column = Resolve(columnCmd.Id, existingColumns, keptColumnIds, "column", () => new TemplateColumn());
        column.Table = table;
        column.Width = columnCmd.Width;
        column.OrderIndex = ci + 1;

        for (var si = 0; si < columnCmd.Sections.Count; si++)
        {
          var sectionCmd = columnCmd.Sections[si];
          var section = Resolve(sectionCmd.Id, existingSections, keptSectionIds, "section", () => new TemplateSection());
          section.Column = column;
          section.Title = sectionCmd.Title.Trim();
          section.OrderIndex = si + 1;

          for (var ii = 0; ii < sectionCmd.Items.Count; ii++)
          {
            var itemCmd = sectionCmd.Items[ii];
            var item = Resolve(itemCmd.Id, existingItems, keptItemIds, "attribute", () => new TemplateItem());
            if (itemCmd.Id != null && item.Type != itemCmd.Type)
              throw new BadRequestException(
                $"Attribute '{item.Key}': the type of an existing attribute cannot be changed (remove it and add a new one)"
              );
            item.Section = section;
            item.Key = itemCmd.Key.Trim();
            item.Title = itemCmd.Title.Trim();
            item.Type = itemCmd.Type;
            item.ValidatingRegex = string.IsNullOrWhiteSpace(itemCmd.ValidatingRegex) ? null : itemCmd.ValidatingRegex;
            item.OrderIndex = ii + 1;
          }
        }
      }
    }

    // Values of removed attributes go too (ItemFieldValue -> TemplateItem is Restrict).
    var removedItemIds = existingItems.Keys.Where(id => !keptItemIds.Contains(id)).ToList();
    var deletedValues = 0;
    if (removedItemIds.Count > 0)
    {
      var values = await dbContext.ItemFieldValues //
        .Where(v => removedItemIds.Contains(v.TemplateItemId))
        .ToListAsync(cancellationToken);
      deletedValues = values.Count;
      dbContext.ItemFieldValues.RemoveRange(values);
    }

    // Removed elements are removed explicitly (EF orders the deletes by dependency); anything that was
    // moved out of a removed parent has already been re-parented above.
    dbContext.TemplateItems.RemoveRange(existingItems.Where(kv => !keptItemIds.Contains(kv.Key)).Select(kv => kv.Value));
    dbContext.TemplateSections.RemoveRange(existingSections.Where(kv => !keptSectionIds.Contains(kv.Key)).Select(kv => kv.Value));
    dbContext.TemplateColumns.RemoveRange(existingColumns.Where(kv => !keptColumnIds.Contains(kv.Key)).Select(kv => kv.Value));
    dbContext.TemplateTables.RemoveRange(existingTables.Where(kv => !keptTableIds.Contains(kv.Key)).Select(kv => kv.Value));

    await dbContext.SaveChangesAsync(cancellationToken);

    return new Result(deletedValues);
  }

  private static T Resolve<T>(int? id, Dictionary<int, T> existing, HashSet<int> kept, string what, Func<T> create)
    where T : class
  {
    if (id is not int realId)
      return create();

    if (!existing.TryGetValue(realId, out var entity))
      throw new BadRequestException($"The {what} with id {realId} does not belong to this template");
    if (!kept.Add(realId))
      throw new BadRequestException($"The {what} with id {realId} is listed more than once");
    return entity;
  }

  /// <summary>Structural validation; the sum of column widths is intentionally NOT checked (12 is expected, not required).</summary>
  private static void Validate(Command command)
  {
    var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var table in command.Tables)
    {
      if (table.Columns.Count < 1)
        throw new BadRequestException("Every table needs at least one column");

      foreach (var column in table.Columns)
      {
        if (column.Width < 1)
          throw new BadRequestException("Column width must be at least 1");

        foreach (var item in column.Sections.SelectMany(s => s.Items))
        {
          if (string.IsNullOrWhiteSpace(item.Key))
            throw new BadRequestException("Attribute key cannot be empty");
          if (string.IsNullOrWhiteSpace(item.Title))
            throw new BadRequestException($"Attribute '{item.Key}': title cannot be empty");
          if (!keys.Add(item.Key.Trim()))
            throw new BadRequestException($"Duplicate attribute key '{item.Key}'");
          if (!Enum.IsDefined(item.Type))
            throw new BadRequestException($"Attribute '{item.Key}': invalid type '{item.Type}'");
          if (!string.IsNullOrWhiteSpace(item.ValidatingRegex))
          {
            try
            {
              _ = new Regex(item.ValidatingRegex);
            }
            catch (ArgumentException)
            {
              throw new BadRequestException($"Attribute '{item.Key}': invalid validating regex");
            }
          }
        }
      }
    }
  }
}

[EndpointSummary("Replaces a project's template (tables → columns → sections → attributes) for the given item type")]
public class Endpoint : GenericOkEndpoint<Command, Parameters, Handler, Result>
{
  public override HttpMethod Method => HttpMethod.Put;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "{id}/templates/{itemType}";
  public override string[] RequiredRoles => [];
}

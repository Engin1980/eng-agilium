using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Model.Db;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Templates;

/// <summary>
/// One incoming field from a template's full-replace update Command, decoupled from any single
/// feature's own Command/TemplateItemCommand record shape so the validation below can be shared
/// between the project-scoped and global template Update endpoints.
/// </summary>
public record TemplateFieldInput(
  int? Id,
  string Key,
  string Title,
  TemplateItemType Type,
  string? ValidatingRegex,
  int OrderIndex,
  int ColumnStart,
  int ColumnSpan,
  int RowStart,
  int RowSpan
);

public static class TemplateGridValidation
{
  /// <summary>
  /// Validates a full-replace template update and applies it to the tracked `template` (add/update/
  /// remove TemplateItems as needed - removed fields also lose their ItemFieldValues, since that FK is
  /// DeleteBehavior.Restrict).
  /// </summary>
  public static async Task ApplyReplaceAsync(
    AppDbContext dbContext,
    Template template,
    int columnCount,
    IReadOnlyList<TemplateFieldInput> fields,
    CancellationToken cancellationToken
  )
  {
    if (columnCount < 1)
      throw new BadRequestException("ColumnCount must be at least 1");

    var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (var field in fields)
    {
      if (string.IsNullOrWhiteSpace(field.Key))
        throw new BadRequestException("Field key cannot be empty");
      if (!keys.Add(field.Key))
        throw new BadRequestException($"Duplicate field key '{field.Key}'");
      if (!Enum.IsDefined(typeof(TemplateItemType), field.Type))
        throw new BadRequestException($"Invalid field type '{field.Type}'");
      if (field.ColumnStart < 1 || field.ColumnSpan < 1 || field.RowStart < 1 || field.RowSpan < 1)
        throw new BadRequestException($"Field '{field.Key}': grid position values must be at least 1");
      if (field.ColumnStart + field.ColumnSpan - 1 > columnCount)
        throw new BadRequestException($"Field '{field.Key}' extends past the template's {columnCount} columns");
    }

    var incomingIds = fields.Where(f => f.Id.HasValue).Select(f => f.Id!.Value).ToHashSet();
    var toRemove = template.TemplateItems.Where(ti => !incomingIds.Contains(ti.Id)).ToList();
    if (toRemove.Count > 0)
    {
      var removedIds = toRemove.Select(ti => ti.Id).ToList();
      var valuesToRemove = await dbContext
        .ItemFieldValues.Where(v => removedIds.Contains(v.TemplateItemId))
        .ToListAsync(cancellationToken);
      dbContext.ItemFieldValues.RemoveRange(valuesToRemove);
      dbContext.TemplateItems.RemoveRange(toRemove);
    }

    foreach (var field in fields)
    {
      if (field.Id is int id)
      {
        var existing =
          template.TemplateItems.FirstOrDefault(ti => ti.Id == id)
          ?? throw new BadRequestException($"Field id {id} does not belong to this template");
        existing.Key = field.Key;
        existing.Title = field.Title;
        existing.Type = field.Type;
        existing.ValidatingRegex = field.ValidatingRegex;
        existing.OrderIndex = field.OrderIndex;
        existing.ColumnStart = field.ColumnStart;
        existing.ColumnSpan = field.ColumnSpan;
        existing.RowStart = field.RowStart;
        existing.RowSpan = field.RowSpan;
      }
      else
      {
        template.TemplateItems.Add(
          new TemplateItem
          {
            Key = field.Key,
            Title = field.Title,
            Type = field.Type,
            ValidatingRegex = field.ValidatingRegex,
            OrderIndex = field.OrderIndex,
            ColumnStart = field.ColumnStart,
            ColumnSpan = field.ColumnSpan,
            RowStart = field.RowStart,
            RowSpan = field.RowSpan,
          }
        );
      }
    }

    template.ColumnCount = columnCount;
  }
}

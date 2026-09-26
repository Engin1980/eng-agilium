using System.Globalization;
using System.Text.RegularExpressions;
using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Model.Db;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Items.SetFieldValues;

public record FieldValueCommand(int TemplateItemId, string? Value);

public record Command(List<FieldValueCommand> Values);

public class Handler(AppDbContext dbContext) : GenericHandler<Command, IdParameters, EmptyResult>
{
  public override async Task<EmptyResult> HandleAsync(
    Command command,
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

    var templateItemsById = template.TemplateItems.ToDictionary(ti => ti.Id);

    var existingByTemplateItemId = await dbContext
      .ItemFieldValues.Where(v => v.ItemId == item.Id)
      .ToDictionaryAsync(v => v.TemplateItemId, cancellationToken);

    foreach (var incoming in command.Values)
    {
      // Every value must belong to this item's own (project, type) template - otherwise a caller could
      // write values against another project's or another item type's fields.
      if (!templateItemsById.TryGetValue(incoming.TemplateItemId, out var templateItem))
        throw new BadRequestException(
          $"Template item {incoming.TemplateItemId} does not belong to this item's template"
        );

      var normalized = NormalizeAndValidate(templateItem, incoming.Value);

      if (existingByTemplateItemId.TryGetValue(incoming.TemplateItemId, out var existingRow))
      {
        if (normalized == null)
          dbContext.ItemFieldValues.Remove(existingRow);
        else
          existingRow.Value = normalized;
      }
      else if (normalized != null)
      {
        dbContext.ItemFieldValues.Add(
          new ItemFieldValue
          {
            ItemId = item.Id,
            TemplateItemId = incoming.TemplateItemId,
            Value = normalized,
          }
        );
      }
    }

    await dbContext.SaveChangesAsync(cancellationToken);

    return new EmptyResult();
  }

  /// <summary>Validates a raw value against its field's TemplateItemType/ValidatingRegex, returning the
  /// value to persist (null clears the field; numeric values are normalized to invariant-culture form,
  /// accepting a comma decimal separator since that's what a Czech user will type).</summary>
  private static string? NormalizeAndValidate(TemplateItem templateItem, string? rawValue)
  {
    if (templateItem.Type == TemplateItemType.LabelOnly)
    {
      if (!string.IsNullOrEmpty(rawValue))
        throw new BadRequestException($"Field '{templateItem.Key}' is label-only and cannot hold a value");
      return null;
    }

    if (string.IsNullOrEmpty(rawValue))
      return null;

    switch (templateItem.Type)
    {
      case TemplateItemType.InlineInt:
      case TemplateItemType.NextlineInt:
        if (!int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
          throw new BadRequestException($"Field '{templateItem.Key}' expects a whole number");
        break;

      case TemplateItemType.InlineDouble:
      case TemplateItemType.NNextlineDouble:
        rawValue = rawValue.Replace(',', '.');
        if (!double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
          throw new BadRequestException($"Field '{templateItem.Key}' expects a decimal number");
        break;

      case TemplateItemType.Checkbox:
        if (rawValue != "true" && rawValue != "false")
          throw new BadRequestException($"Field '{templateItem.Key}' expects 'true' or 'false'");
        break;

      default:
        break; // free-text types: InlineText, NextlineText, NextlineTextArea, Comments, Untemplated
    }

    if (!string.IsNullOrEmpty(templateItem.ValidatingRegex) && !Regex.IsMatch(rawValue, templateItem.ValidatingRegex))
      throw new BadRequestException($"Field '{templateItem.Key}' does not match the required format");

    return rawValue;
  }
}

[EndpointSummary("Sets (creates/updates/clears) an item's field values according to its template")]
public class Endpoint : GenericOkEndpoint<Command, IdParameters, Handler, EmptyResult>
{
  public override HttpMethod Method => HttpMethod.Put;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "items/{id}/fields";
  public override string[] RequiredRoles => [];
}

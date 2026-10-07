namespace Eng.Agilium.Be.Model.Db;

/// <summary>An attribute of a template: a title plus a typed value, shown across the full width of its section's column.</summary>
public class TemplateItem
{
  public int Id { get; set; }
  public int TemplateSectionId { get; set; }
  public TemplateSection Section { get; set; } = null!;

  public string Key { get; set; } = string.Empty;
  public string Title { get; set; } = string.Empty;
  public TemplateItemType Type { get; set; }
  public string? ValidatingRegex { get; set; }
  public int OrderIndex { get; set; }
}

public enum TemplateItemType
{
  SingleLineText = 1,
  MultiLineText = 2,
  Integer = 3,
  Decimal = 4,
  Boolean = 5,

  /// <summary>Special component for comments on the item (stored as plain text for now).</summary>
  Comments = 6,

  /// <summary>Just a text label, no value.</summary>
  LabelOnly = 7,
}

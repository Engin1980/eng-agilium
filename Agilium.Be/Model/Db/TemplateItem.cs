namespace Eng.Agilium.Be.Model.Db;

public class TemplateItem
{
  public int Id { get; set; }
  public int TemplateId { get; set; }
  public Template Template { get; set; } = null!;

  public string Key { get; set; } = string.Empty;
  public string Title { get; set; } = string.Empty;
  public TemplateItemType Type { get; set; }
  public string? ValidatingRegex { get; set; }

  /// <summary>Tie-breaker for stable ordering (tab order, list rendering) independent of grid position.</summary>
  public int OrderIndex { get; set; }

  // CSS grid positioning, 1-based - mirrors `grid-column`/`grid-row` (`<start> / span <span>`).
  public int ColumnStart { get; set; } = 1;
  public int ColumnSpan { get; set; } = 1;
  public int RowStart { get; set; } = 1;
  public int RowSpan { get; set; } = 1;
}

public enum TemplateItemType
{
  InlineText = 1,
  NextlineText = 2,
  NextlineTextArea = 3,
  InlineInt = 4,
  NextlineInt = 5,
  InlineDouble = 6,
  NNextlineDouble = 7,
  Comments = 8,
  Untemplated = 9,
  Checkbox = 10,
  LabelOnly = 11,
}

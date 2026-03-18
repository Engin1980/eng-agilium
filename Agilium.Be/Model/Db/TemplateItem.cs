namespace Eng.Agilium.Be.Model.Db;

public class TemplateItem
{
  public int Id { get; set; }
  public string Key { get; set; } = string.Empty;
  public int OrderIndex { get; set; }
  public int TemplateColumnId { get; set; }
  public TemplateColumn TemplateColumn { get; set; } = null!;
  public string Title { get; set; } = string.Empty;
  public TemplateItemType Type { get; set; }
  public string? ValidatingRegex { get; set; }
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
}

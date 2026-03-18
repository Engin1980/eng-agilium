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
  InlineText,
  NextlineText,
  NextlineTextArea,
  InlineInt,
  NextlineInt,
  InlineDouble,
  NExtlineDouble,
  Comments,
  Untemplated
}

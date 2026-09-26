namespace Eng.Agilium.Be.Model.Db;

public class Template
{
  public int Id { get; set; }

  /// <summary>Null marks the single global default template for this <see cref="Type"/>.</summary>
  public int? ProjectId { get; set; }
  public Project? Project { get; set; }

  public ItemType Type { get; set; }

  /// <summary>Number of CSS grid columns items of this template are positioned into.</summary>
  public int ColumnCount { get; set; } = 4;

  public ICollection<TemplateItem> TemplateItems { get; set; } = [];
}

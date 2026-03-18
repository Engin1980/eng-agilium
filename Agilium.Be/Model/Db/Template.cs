namespace Eng.Agilium.Be.Model.Db;

public class Template
{
  public int Id { get; set; }
  public int ProjectId { get; set; }
  public Project Project { get; set; } = null!;
  public ItemType Type { get; set; }
  public ICollection<TemplateColumn> TemplateColumns { get; set; } = [];
}


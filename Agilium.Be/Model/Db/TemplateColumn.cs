namespace Eng.Agilium.Be.Model.Db;

public class TemplateColumn
{
  public int Id { get; set; }
  public int TemplateId { get; set; }
  public Template Template { get; set; } = null!;
  public int WidthWeight { get; set; }
  public ICollection<TemplateItem> TemplateItems { get; set; } = [];
}

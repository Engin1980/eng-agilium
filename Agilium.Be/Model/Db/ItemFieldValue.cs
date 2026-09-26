namespace Eng.Agilium.Be.Model.Db;

/// <summary>The value an <see cref="Item"/> holds for one field (<see cref="TemplateItem"/>) of its template.</summary>
public class ItemFieldValue
{
  public int Id { get; set; }
  public int ItemId { get; set; }
  public Item Item { get; set; } = null!;
  public int TemplateItemId { get; set; }
  public TemplateItem TemplateItem { get; set; } = null!;
  public string? Value { get; set; }
}

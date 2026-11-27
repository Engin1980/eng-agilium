namespace Eng.Agilium.Be.Model.Db;

/// <summary>A comment (Markdown text) written into a <c>Comments</c> element of an item's template.</summary>
public class ItemComment
{
  public const int MaxTextLength = 4000;

  public int Id { get; set; }
  public int ItemId { get; set; }
  public Item Item { get; set; } = null!;

  /// <summary>The template element of type <c>Comments</c> the comment belongs to (one thread per element).</summary>
  public int TemplateItemId { get; set; }
  public TemplateItem TemplateItem { get; set; } = null!;
  public int AuthorId { get; set; }
  public AppUser Author { get; set; } = null!;
  public string Text { get; set; } = string.Empty;
  public DateTime CreatedAt { get; set; }
  public DateTime? EditedAt { get; set; }
}

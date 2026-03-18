namespace Eng.Agilium.Be.Model.Db;

public class Item
{
  public int Id { get; set; }
  public int ProjectId { get; set; }
  public string Title { get; set; } = string.Empty;

  public ItemType Type { get; set; }

  public int? ParentId { get; set; }
  public Item? Parent { get; set; }

  public int? AssigneeId { get; set; }
  public AppUser? Assignee { get; set; }
}

public enum ItemType
{
  Task = 1,
  Bug = 2,
  UserStory = 3,
  Feature = 4,
  Epic = 5,
}

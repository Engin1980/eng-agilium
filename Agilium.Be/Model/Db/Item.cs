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
  Task = 0,
  Bug = 1,
  UserStory = 2,
  Feature = 3,
  Epic = 4,
}

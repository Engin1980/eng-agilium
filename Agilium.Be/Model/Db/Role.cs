namespace Eng.Agilium.Be.Model.Db;

public class Role
{
  public int Id { get; set; }
  public string Title { get; set; } = string.Empty;
  public int ProjectId { get; set; }
  public Project Project { get; set; } = null!;

  public bool CanViewProject { get; set; }
  public bool CanManageProject { get; set; }
  public bool CanViewMembers { get; set; }
  public bool CanManageMembers { get; set; }
  public bool CanManageSprints { get; set; }
}

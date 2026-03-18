namespace Eng.Agilium.Be.Model.Db;

public class Membership
{
  public int Id { get; set; }
  public int ProjectId { get; set; }
  public Project Project { get; set; } = null!;
  public int UserId { get; set; }
  public AppUser User { get; set; } = null!;
  public int RoleId { get; set; }
  public Role Role { get; set; } = null!;
}

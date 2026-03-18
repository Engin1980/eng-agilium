using System.Text;

namespace Eng.Agilium.Be.Model.Db;

public interface IRoleAssignment
{
  public int ProjectId { get; set; }
  public bool CanViewProject { get; set; }
  public bool CanManageProject { get; set; }
  public bool CanViewMembers { get; set; }
  public bool CanManageMembers { get; set; }
  public bool CanManageSprints { get; set; }

  public string ToRoleString()
  {
    StringBuilder ret = new();
    ret.Append('P').Append(ProjectId).Append(':');
    ret.Append(CanViewProject ? "1" : "0");
    ret.Append(CanManageProject ? "1" : "0");
    ret.Append(CanViewMembers ? "1" : "0");
    ret.Append(CanManageMembers ? "1" : "0");
    ret.Append(CanManageSprints ? "1" : "0");
    return ret.ToString();
  }

  public static IRoleAssignment FromRoleString(string roleString)
  {
    string[] parts = roleString.Split(':');
    if (parts.Length != 2 || !parts[0].StartsWith("P"))
      throw new ArgumentException("Invalid role string format.");
    int projectId = int.Parse(parts[0].Substring(1));
    string permissions = parts[1];
    if (permissions.Length != 5)
      throw new ArgumentException("Invalid permissions format.");
    return new RoleAsignment
    {
      ProjectId = projectId,
      CanViewProject = permissions[0] == '1',
      CanManageProject = permissions[1] == '1',
      CanViewMembers = permissions[2] == '1',
      CanManageMembers = permissions[3] == '1',
      CanManageSprints = permissions[4] == '1'
    };
  }
}

public class RoleAsignment : IRoleAssignment
{
  public int ProjectId { get; set; }
  public bool CanViewProject { get; set; }
  public bool CanManageProject { get; set; }
  public bool CanViewMembers { get; set; }
  public bool CanManageMembers { get; set; }
  public bool CanManageSprints { get; set; }

}


public class Role : IRoleAssignment
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


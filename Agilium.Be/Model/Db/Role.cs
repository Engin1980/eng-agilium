namespace Eng.Agilium.Be.Model.Db;

/// <summary>
/// System roles every project gets on creation. They cannot be deleted or renamed, and the project-wide
/// "owner only" rights (renaming and deleting the project, appointing owners) belong to <see cref="Owner"/>
/// by kind, not by a flag. See .docs/development/roles.md.
/// </summary>
public enum RoleSystemKind
{
  /// <summary>A custom role created in the project.</summary>
  None = 0,
  Owner = 1,
  /// <summary>Everything the owner can do except renaming/deleting the project and managing owners.</summary>
  Manager = 2,
  /// <summary>Works with items and the kanban board, cannot manage sprints or the project.</summary>
  Member = 3,
  Guest = 4,
}

/// <summary>The per-project permission flags of a role.</summary>
public interface IRoleAssignment
{
  public bool CanViewProject { get; set; }
  public bool CanManageProject { get; set; }
  public bool CanViewMembers { get; set; }
  public bool CanManageMembers { get; set; }
  public bool CanManageSprints { get; set; }

  /// <summary>Create/edit/delete items, change assignee/parent/field values, assign items to sprints and move cards.</summary>
  public bool CanEditItems { get; set; }
}

public class Role : IRoleAssignment
{
  public int Id { get; set; }
  public string Title { get; set; } = string.Empty;
  public int ProjectId { get; set; }
  public Project Project { get; set; } = null!;
  public RoleSystemKind SystemKind { get; set; }

  public bool CanViewProject { get; set; }
  public bool CanManageProject { get; set; }
  public bool CanViewMembers { get; set; }
  public bool CanManageMembers { get; set; }
  public bool CanManageSprints { get; set; }
  public bool CanEditItems { get; set; }
}

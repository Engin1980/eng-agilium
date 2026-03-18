namespace Eng.Agilium.Be.Model.Db;

public class Project
{
  public int Id { get; set; }
  public string Title { get; set; } = string.Empty;
  public string Description { get; set; } = string.Empty;
  public ICollection<Role> Roles { get; set; } = [];
  public ICollection<Membership> Memberships { get; set; } = [];
  public ProjectState State { get; set; }
  public ICollection<WorkflowState> WorkflowStates { get; set; } = [];
  public ICollection<Template> Templates { get; set; } = [];
}

public enum ProjectState
{
  Active = 1,
  Inactive = 2,
}

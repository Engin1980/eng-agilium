namespace Eng.Agilium.Be.Model.Db;

public class Project
{
  public int Id { get; set; }
  public string Title { get; set; } = string.Empty;
  public string Description { get; set; } = string.Empty;
  public int OwnerId { get; set; }
  public AppUser Owner { get; set; } = null!;
  public ICollection<Membership> Roles { get; set; } = [];
  public ProjectStatus Status { get; set; }
  public ICollection<WorkflowState> WorkflowStates { get; set; } = [];
}

public enum ProjectStatus
{
  Active,
  Inactive,
}

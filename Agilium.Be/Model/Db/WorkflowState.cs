namespace Eng.Agilium.Be.Model.Db;

public class WorkflowState
{
  public int Id { get; set; }
  public int OrderIndex { get; set; }
  public string Title { get; set; } = string.Empty;
  public string Description { get; set; } = string.Empty;
  public WorkflowStateType Type { get; set; }
  public int ProjectId { get; set; }
  public Project Project { get; set; } = null!;
}

public enum WorkflowStateType
{
  ToDo,
  InProgress,
  Done,
}

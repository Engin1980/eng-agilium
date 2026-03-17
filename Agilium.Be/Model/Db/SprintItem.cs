namespace Eng.Agilium.Be.Model.Db;

public class SprintItem
{
  public int Id { get; set; }

  public int ItemId { get; set; }
  public Item Item { get; set; } = null!;
  public int SprintId { get; set; }
  public Sprint Sprint { get; set; } = null!;
  public int WorkflowStateId { get; set; }
  public WorkflowState WorkflowState { get; set; } = null!;
}

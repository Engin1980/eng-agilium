namespace Eng.Agilium.Be.Model.Db;

public class Sprint
{
  public int Id { get; set; }
  public string Title { get; set; } = string.Empty;
  public int ProjectId { get; set; }
  public Project Project { get; set; } = null!;
  public DateTime? StartDateTime { get; set; }
  public DateTime? EndDateTime { get; set; }
  public DateTime? ExpectedStartDateTime { get; set; }
  public DateTime? ExpectedEndDateTime { get; set; }
  public SprintState State { get; set; }
}

public enum SprintState
{
  Planned = 0,
  Active = 1,
  Completed = 2,
}

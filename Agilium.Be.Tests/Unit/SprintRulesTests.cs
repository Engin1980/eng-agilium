using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Features.Sprints;
using Eng.Agilium.Be.Model.Db;
using Shouldly;

namespace Eng.Agilium.Be.Tests.Unit;

public class SprintRulesTests
{
  private const WorkflowStateType Todo = WorkflowStateType.ToDo;
  private const WorkflowStateType Active = WorkflowStateType.Active;
  private const WorkflowStateType Done = WorkflowStateType.Done;

  [Fact]
  public void DeriveStatus_NoChildren_IsToDo() => SprintRules.DeriveStatus([]).ShouldBe(Todo);

  [Fact]
  public void DeriveStatus_AllToDo_IsToDo() => SprintRules.DeriveStatus([Todo, Todo]).ShouldBe(Todo);

  [Fact]
  public void DeriveStatus_AllDone_IsDone() => SprintRules.DeriveStatus([Done, Done]).ShouldBe(Done);

  [Theory]
  [InlineData(WorkflowStateType.ToDo, WorkflowStateType.Done)]
  [InlineData(WorkflowStateType.Active, WorkflowStateType.Active)]
  [InlineData(WorkflowStateType.Active, WorkflowStateType.ToDo)]
  [InlineData(WorkflowStateType.Active, WorkflowStateType.Done)]
  public void DeriveStatus_Mixed_IsActive(WorkflowStateType a, WorkflowStateType b) =>
    SprintRules.DeriveStatus([a, b]).ShouldBe(Active);

  [Fact]
  public void EnsureValidDates_EndBeforeStart_Throws()
  {
    var start = new DateTime(2026, 1, 10);
    Should.Throw<BadRequestException>(() => SprintRules.EnsureValidDates(start, start.AddDays(-1)));
  }

  [Theory]
  [InlineData(10, 10)]
  [InlineData(10, 11)]
  public void EnsureValidDates_EndNotBeforeStart_Passes(int startDay, int endDay) =>
    Should.NotThrow(() => SprintRules.EnsureValidDates(new DateTime(2026, 1, startDay), new DateTime(2026, 1, endDay)));

  [Fact]
  public void EnsureValidDates_MissingEnd_Passes() =>
    Should.NotThrow(() => SprintRules.EnsureValidDates(new DateTime(2026, 1, 10), null));
}

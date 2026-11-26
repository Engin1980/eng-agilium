using Eng.Agilium.Be.Model.Db;
using Eng.Agilium.Be.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Eng.Agilium.Be.Tests;

public class SprintsTests(ApiFixture fixture) : ApiTestBase(fixture)
{
  private const int Task = (int)ItemType.Task;
  private const int Story = (int)ItemType.UserStory;
  private const int Feature = (int)ItemType.Feature;

  private async Task<(TestApi user, int project)> ArrangeAsync()
  {
    var user = await Api.RegisterAndLoginAsync();
    return (user, await user.CreateProjectAsync());
  }

  private Task<int> StateIdAsync(int project, WorkflowStateType type) =>
    Factory.WithDbAsync(db =>
      db.WorkflowStates.Where(w => w.ProjectId == project && w.Type == type).OrderBy(w => w.OrderIndex).Select(w => w.Id).FirstAsync()
    );

  [Fact]
  public async Task Create_DuplicateTitleInProject_Returns409_ButSameTitleInAnotherProjectIsFine()
  {
    var (user, project) = await ArrangeAsync();
    var other = await user.CreateProjectAsync();
    await user.CreateSprintAsync(project, "Sprint 1");

    var duplicate = await user.Post("projects/sprints", new { projectId = project, title = "Sprint 1" });
    duplicate.Code.ShouldBe(409);
    (await user.Post("projects/sprints", new { projectId = other, title = "Sprint 1" })).Code.ShouldBe(201);
  }

  [Fact]
  public async Task Create_EndBeforeStart_Returns400_AndNewSprintIsPlanned()
  {
    var (user, project) = await ArrangeAsync();
    var bad = await user.Post(
      "projects/sprints",
      new { projectId = project, title = "S", startDateTime = "2026-02-10T00:00:00", endDateTime = "2026-02-01T00:00:00" }
    );
    bad.Code.ShouldBe(400);

    var id = await user.CreateSprintAsync(project, "Ok");
    var list = await user.Get($"projects/{project}/sprints");
    list.Json.GetProperty("sprints").EnumerateArray().Single(s => s.GetProperty("id").GetInt32() == id)
      .GetProperty("state").GetInt32().ShouldBe((int)SprintState.Planned);
  }

  [Fact]
  public async Task Delete_OnlyEmptySprint()
  {
    var (user, project) = await ArrangeAsync();
    var task = await user.CreateItemAsync(project, "T", Task);
    var full = await user.CreateSprintAsync(project);
    var empty = await user.CreateSprintAsync(project);
    (await user.Post($"projects/sprints/{full}/items", new { itemId = task })).Code.ShouldBe(204);

    (await user.Delete($"projects/sprints/{full}")).Code.ShouldBe(400);
    (await user.Delete($"projects/sprints/{empty}")).Code.ShouldBe(204);
  }

  [Fact]
  public async Task AssignItem_MovesTheItemBetweenSprintsAndKeepsItsState()
  {
    var (user, project) = await ArrangeAsync();
    var task = await user.CreateItemAsync(project, "T", Task);
    var s1 = await user.CreateSprintAsync(project);
    var s2 = await user.CreateSprintAsync(project);
    var done = await StateIdAsync(project, WorkflowStateType.Done);

    (await user.Post($"projects/sprints/{s1}/items", new { itemId = task })).Code.ShouldBe(204);
    (await user.Patch($"projects/sprints/{s1}/items/{task}/state", new { workflowStateId = done })).Code.ShouldBe(204);
    (await user.Post($"projects/sprints/{s2}/items", new { itemId = task })).Code.ShouldBe(204);

    await Factory.WithDbAsync(async db =>
    {
      var sprintItem = await db.SprintItems.SingleAsync(si => si.ItemId == task);
      sprintItem.SprintId.ShouldBe(s2);
      sprintItem.WorkflowStateId.ShouldBe(done);
    });
  }

  [Fact]
  public async Task AssignItem_FeaturesAndStoriesCannotBeAssigned_NorToACompletedSprint()
  {
    var (user, project) = await ArrangeAsync();
    var feature = await user.CreateItemAsync(project, "F", Feature);
    var story = await user.CreateItemAsync(project, "S", Story, feature);
    var task = await user.CreateItemAsync(project, "T", Task, story);
    var sprint = await user.CreateSprintAsync(project, "S1");

    (await user.Post($"projects/sprints/{sprint}/items", new { itemId = feature })).Code.ShouldBe(400);
    (await user.Post($"projects/sprints/{sprint}/items", new { itemId = story })).Code.ShouldBe(400);

    (await user.Patch($"projects/sprints/{sprint}", new { title = "S1", state = (int)SprintState.Completed })).Code.ShouldBe(204);
    (await user.Post($"projects/sprints/{sprint}/items", new { itemId = task })).Code.ShouldBe(400);
  }

  [Fact]
  public async Task AssignItem_StartsInTheFirstToDoState_UnassignRemovesIt()
  {
    var (user, project) = await ArrangeAsync();
    var task = await user.CreateItemAsync(project, "T", Task);
    var sprint = await user.CreateSprintAsync(project);
    var todo = await StateIdAsync(project, WorkflowStateType.ToDo);

    (await user.Post($"projects/sprints/{sprint}/items", new { itemId = task })).Code.ShouldBe(204);
    (await Factory.WithDbAsync(db => db.SprintItems.SingleAsync(si => si.ItemId == task))).WorkflowStateId.ShouldBe(todo);

    (await user.Delete($"projects/sprints/{sprint}/items/{task}")).Code.ShouldBe(204);
    (await Factory.WithDbAsync(db => db.SprintItems.CountAsync())).ShouldBe(0);
  }

  [Fact]
  public async Task UpdateItemState_UnknownOrForeignState_IsRejected()
  {
    var (user, project) = await ArrangeAsync();
    var otherProject = await user.CreateProjectAsync();
    var task = await user.CreateItemAsync(project, "T", Task);
    var sprint = await user.CreateSprintAsync(project);
    (await user.Post($"projects/sprints/{sprint}/items", new { itemId = task })).Code.ShouldBe(204);
    var foreignState = await StateIdAsync(otherProject, WorkflowStateType.Done);

    var response = await user.Patch($"projects/sprints/{sprint}/items/{task}/state", new { workflowStateId = foreignState });
    response.Code.ShouldBeOneOf(400, 404);
  }

  [Fact]
  public async Task GetBoard_DerivesParentStatusFromChildrenInThatSprint()
  {
    var (user, project) = await ArrangeAsync();
    var feature = await user.CreateItemAsync(project, "F", Feature);
    var story = await user.CreateItemAsync(project, "S", Story, feature);
    var t1 = await user.CreateItemAsync(project, "T1", Task, story);
    var t2 = await user.CreateItemAsync(project, "T2", Task, story);
    var sprint = await user.CreateSprintAsync(project);
    var done = await StateIdAsync(project, WorkflowStateType.Done);

    // no child in the sprint yet -> the feature is not on the board
    var empty = await user.Get($"projects/sprints/{sprint}/board");
    empty.Json.GetProperty("features").GetArrayLength().ShouldBe(0);

    await user.Post($"projects/sprints/{sprint}/items", new { itemId = t1 });
    await user.Post($"projects/sprints/{sprint}/items", new { itemId = t2 });

    int StatusOf(ApiResponse board) =>
      board.Json.GetProperty("features")[0].GetProperty("status").GetInt32();

    StatusOf(await user.Get($"projects/sprints/{sprint}/board")).ShouldBe((int)WorkflowStateType.ToDo);

    await user.Patch($"projects/sprints/{sprint}/items/{t1}/state", new { workflowStateId = done });
    StatusOf(await user.Get($"projects/sprints/{sprint}/board")).ShouldBe((int)WorkflowStateType.Active);

    await user.Patch($"projects/sprints/{sprint}/items/{t2}/state", new { workflowStateId = done });
    StatusOf(await user.Get($"projects/sprints/{sprint}/board")).ShouldBe((int)WorkflowStateType.Done);
  }

  [Fact]
  public async Task ListBacklog_ShowsTasksWithTheirSprint()
  {
    var (user, project) = await ArrangeAsync();
    var t1 = await user.CreateItemAsync(project, "T1", Task);
    await user.CreateItemAsync(project, "T2", Task);
    var sprint = await user.CreateSprintAsync(project, "Sp");
    await user.Post($"projects/sprints/{sprint}/items", new { itemId = t1 });

    var backlog = await user.Get($"projects/{project}/backlog");
    backlog.Code.ShouldBe(200);
    var items = backlog.Json.GetProperty("items").EnumerateArray().ToList();
    items.Count.ShouldBe(2);
    items.Single(i => i.GetProperty("id").GetInt32() == t1).GetProperty("sprintTitle").GetString().ShouldBe("Sp");
  }
}

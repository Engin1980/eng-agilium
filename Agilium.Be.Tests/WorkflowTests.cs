using Eng.Agilium.Be.Model.Db;
using Eng.Agilium.Be.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Eng.Agilium.Be.Tests;

public class WorkflowTests(ApiFixture fixture) : ApiTestBase(fixture)
{
  private const int ToDo = (int)WorkflowStateType.ToDo;
  private const int Active = (int)WorkflowStateType.Active;
  private const int Done = (int)WorkflowStateType.Done;

  private record State(int Id, string Title, int Type);

  private async Task<(TestApi user, int project, List<State> states)> ArrangeAsync()
  {
    var user = await Api.RegisterAndLoginAsync();
    var project = await user.CreateProjectAsync();
    return (user, project, await StatesAsync(user, project));
  }

  private static async Task<List<State>> StatesAsync(TestApi user, int project)
  {
    var response = await user.Get($"projects/{project}/workflow");
    return response
      .Json.GetProperty("states")
      .EnumerateArray()
      .Select(s => new State(s.GetProperty("id").GetInt32(), s.GetProperty("title").GetString()!, s.GetProperty("type").GetInt32()))
      .ToList();
  }

  [Fact]
  public async Task Get_NewProjectHasToDoActiveDone()
  {
    var (_, _, states) = await ArrangeAsync();
    states.Select(s => s.Type).ShouldBe([ToDo, Active, Done]);
  }

  [Theory]
  [InlineData("one")]
  [InlineData("noDone")]
  [InlineData("noToDo")]
  public async Task Update_InvalidWorkflow_Returns400(string kind)
  {
    var (user, project, _) = await ArrangeAsync();
    object[] states = kind switch
    {
      "one" => [new { title = "Only", type = ToDo }],
      "noDone" => [new { title = "A", type = ToDo }, new { title = "B", type = Active }],
      _ => [new { title = "A", type = Active }, new { title = "B", type = Done }],
    };

    (await user.Put($"projects/{project}/workflow", new { states })).Code.ShouldBe(400);
  }

  [Fact]
  public async Task Update_ForeignOrDuplicateStateId_Returns400()
  {
    var (user, project, states) = await ArrangeAsync();
    var other = await user.CreateProjectAsync();
    var foreign = (await StatesAsync(user, other))[0];

    var withForeign = new object[] { new { id = foreign.Id, title = "X", type = ToDo }, new { title = "D", type = Done } };
    (await user.Put($"projects/{project}/workflow", new { states = withForeign })).Code.ShouldBe(400);

    var duplicate = new object[]
    {
      new { id = states[0].Id, title = "A", type = ToDo },
      new { id = states[0].Id, title = "B", type = Done },
    };
    (await user.Put($"projects/{project}/workflow", new { states = duplicate })).Code.ShouldBe(400);
  }

  [Fact]
  public async Task Update_RemovedColumn_MovesItemsToTheNearestLeftColumn()
  {
    var (user, project, states) = await ArrangeAsync();
    var task = await user.CreateItemAsync(project, "T", (int)ItemType.Task);
    var sprint = await user.CreateSprintAsync(project);
    await user.Post($"projects/sprints/{sprint}/items", new { itemId = task });
    var active = states.Single(s => s.Type == Active);
    await user.Patch($"projects/sprints/{sprint}/items/{task}/state", new { workflowStateId = active.Id });

    // drop the Active column
    var kept = states.Where(s => s.Type != Active).Select(s => new { id = (int?)s.Id, title = s.Title, type = s.Type }).ToArray();
    var response = await user.Put($"projects/{project}/workflow", new { states = kept });

    response.Code.ShouldBe(200);
    response.Json.GetProperty("movedItems").GetInt32().ShouldBe(1);
    var todo = states.Single(s => s.Type == ToDo);
    (await Factory.WithDbAsync(db => db.SprintItems.SingleAsync(si => si.ItemId == task))).WorkflowStateId.ShouldBe(todo.Id);
  }

  [Fact]
  public async Task Update_RemovedFirstColumn_MovesItemsToTheNextOne()
  {
    var (user, project, states) = await ArrangeAsync();
    var task = await user.CreateItemAsync(project, "T", (int)ItemType.Task);
    var sprint = await user.CreateSprintAsync(project);
    await user.Post($"projects/sprints/{sprint}/items", new { itemId = task }); // starts in ToDo (first column)

    var kept = new object[]
    {
      new { id = (int?)states[1].Id, title = "Doing", type = ToDo },
      new { id = (int?)states[2].Id, title = "Done", type = Done },
    };
    (await user.Put($"projects/{project}/workflow", new { states = kept })).Code.ShouldBe(200);

    (await Factory.WithDbAsync(db => db.SprintItems.SingleAsync(si => si.ItemId == task))).WorkflowStateId.ShouldBe(states[1].Id);
  }

  [Fact]
  public async Task Update_RetypingAColumn_ChangesTheDerivedParentStatus()
  {
    var (user, project, states) = await ArrangeAsync();
    var feature = await user.CreateItemAsync(project, "F", (int)ItemType.Feature);
    var story = await user.CreateItemAsync(project, "S", (int)ItemType.UserStory, feature);
    var task = await user.CreateItemAsync(project, "T", (int)ItemType.Task, story);
    var sprint = await user.CreateSprintAsync(project);
    await user.Post($"projects/sprints/{sprint}/items", new { itemId = task });
    var active = states.Single(s => s.Type == Active);
    await user.Patch($"projects/sprints/{sprint}/items/{task}/state", new { workflowStateId = active.Id });

    var board = await user.Get($"projects/sprints/{sprint}/board");
    board.Json.GetProperty("features")[0].GetProperty("status").GetInt32().ShouldBe(Active);

    var retyped = states.Select(s => new { id = (int?)s.Id, title = s.Title, type = s.Id == active.Id ? Done : s.Type }).ToArray();
    (await user.Put($"projects/{project}/workflow", new { states = retyped })).Code.ShouldBe(200);

    board = await user.Get($"projects/sprints/{sprint}/board");
    board.Json.GetProperty("features")[0].GetProperty("status").GetInt32().ShouldBe(Done);
  }
}

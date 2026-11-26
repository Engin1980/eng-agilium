using Eng.Agilium.Be.Model.Db;
using Eng.Agilium.Be.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Eng.Agilium.Be.Tests;

public class ProjectsTests(ApiFixture fixture) : ApiTestBase(fixture)
{
  [Fact]
  public async Task Create_SetsUpRolesMembershipWorkflowAndTemplates()
  {
    var user = await Api.RegisterAndLoginAsync();
    var projectId = await user.CreateProjectAsync("Alpha");

    await Factory.WithDbAsync(async db =>
    {
      var roles = await db.Roles.Where(r => r.ProjectId == projectId).ToListAsync();
      roles.Select(r => r.Title).ShouldContain("Owner");
      roles.Select(r => r.Title).ShouldContain("Guest");

      var membership = await db.Memberships.Include(m => m.Role).SingleAsync(m => m.Role.ProjectId == projectId);
      membership.UserId.ShouldBe(user.UserId);
      membership.Role.Title.ShouldBe("Owner");

      var states = await db.WorkflowStates.Where(w => w.ProjectId == projectId).ToListAsync();
      states.Select(s => s.Type).ShouldBe(
        [WorkflowStateType.ToDo, WorkflowStateType.Active, WorkflowStateType.Done],
        ignoreOrder: true
      );

      var templates = await db.Templates.Where(t => t.ProjectId == projectId).ToListAsync();
      templates.Select(t => t.Type).ShouldBe(Enum.GetValues<ItemType>(), ignoreOrder: true);
    });
  }

  [Fact]
  public async Task Create_DuplicateTitle_Returns409()
  {
    var user = await Api.RegisterAndLoginAsync();
    await user.CreateProjectAsync("Same");
    var response = await user.Post("projects", new { title = "Same", description = "" });
    response.Code.ShouldBe(409);
  }

  [Fact]
  public async Task Create_EmptyTitle_Returns400()
  {
    var user = await Api.RegisterAndLoginAsync();
    var response = await user.Post("projects", new { title = "", description = "" });
    response.Code.ShouldBe(400);
    response.ErrorKey.ShouldBe("COMMAND_VALIDATION_EXCEPTION");
  }

  [Fact]
  public async Task Update_ChangesTitleAndDescription_DuplicateTitleReturns409()
  {
    var user = await Api.RegisterAndLoginAsync();
    var a = await user.CreateProjectAsync("A");
    await user.CreateProjectAsync("B");

    (await user.Patch($"projects/{a}", new { title = "A2", description = "d2" })).Code.ShouldBe(204);
    (await user.Patch($"projects/{a}", new { title = "B", description = "d2" })).Code.ShouldBe(409);
  }

  [Fact]
  public async Task UpdateStatus_ChangesTheProjectState()
  {
    var user = await Api.RegisterAndLoginAsync();
    var id = await user.CreateProjectAsync("S");

    (await user.Patch($"projects/{id}/state", new { status = (int)ProjectState.Inactive })).Code.ShouldBe(204);

    var list = await user.Get($"projects?memberId={user.UserId}");
    list.Json.GetProperty("projects").EnumerateArray().Single(p => p.GetProperty("id").GetInt32() == id)
      .GetProperty("status").GetInt32().ShouldBe((int)ProjectState.Inactive);
  }

  [Fact]
  public async Task List_ByMemberId_ReturnsOnlyProjectsOfThatMember()
  {
    var u1 = await Api.RegisterAndLoginAsync();
    var u2 = await Api.RegisterAndLoginAsync();
    var p1 = await u1.CreateProjectAsync("P1");
    await u2.CreateProjectAsync("P2");

    var list = await u1.Get($"projects?memberId={u1.UserId}");
    list.Json.GetProperty("projects").EnumerateArray().Select(p => p.GetProperty("id").GetInt32()).ShouldBe([p1]);
  }

  [Fact]
  public async Task ListItems_ReturnsTheTreeWithGenericFlags()
  {
    var user = await Api.RegisterAndLoginAsync();
    var project = await user.CreateProjectAsync();
    var feature = await user.CreateItemAsync(project, "F", (int)ItemType.Feature);
    var story = await user.CreateItemAsync(project, "S", (int)ItemType.UserStory, feature);
    await user.CreateItemAsync(project, "T", (int)ItemType.Task, story);
    await user.CreateItemAsync(project, "Orphan", (int)ItemType.Task);

    var response = await user.Get($"projects/{project}/items");
    response.Code.ShouldBe(200);
    var roots = response.Json.GetProperty("items").EnumerateArray().ToList();
    roots.Count.ShouldBe(2); // the real feature and the generic feature
    roots.Count(r => r.GetProperty("isGeneric").GetBoolean()).ShouldBe(1);
    var real = roots.Single(r => !r.GetProperty("isGeneric").GetBoolean());
    real.GetProperty("subItems")[0].GetProperty("subItems")[0].GetProperty("title").GetString().ShouldBe("T");
  }

  [Fact]
  public async Task Options_ReturnTheEnumValues()
  {
    var user = await Api.RegisterAndLoginAsync();
    (await user.Get("projects/state-options")).Json.GetProperty("options").GetArrayLength().ShouldBe(2);
    (await user.Get("items/type-options")).Json.GetProperty("options").GetArrayLength().ShouldBe(4);
  }
}

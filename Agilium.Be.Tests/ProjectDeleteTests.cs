using Eng.Agilium.Be.Model.Db;
using Eng.Agilium.Be.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Eng.Agilium.Be.Tests;

public class ProjectDeleteTests(ApiFixture fixture) : ApiTestBase(fixture)
{
  private Task<int> RoleId(int projectId, RoleSystemKind kind) =>
    Factory.WithDbAsync(db => db.Roles.Where(r => r.ProjectId == projectId && r.SystemKind == kind).Select(r => r.Id).SingleAsync());

  private async Task<TestApi> JoinAsync(TestApi owner, int projectId, RoleSystemKind kind)
  {
    var roleId = await RoleId(projectId, kind);
    var key = (await owner.Post($"projects/{projectId}/invitations", new { roleId })).Json.GetProperty("key").GetString()!;
    var user = await Api.RegisterAndLoginAsync();
    (await user.Post("projects/join", new { key })).Code.ShouldBe(200);
    return user;
  }

  private sealed record Counts(int Items, int Sprints, int SprintItems, int Values, int Templates, int TemplateItems, int States, int Roles, int Members, int Invitations, int Projects);

  private Task<Counts> CountAsync(int? projectId) =>
    Factory.WithDbAsync(async db => new Counts(
      await db.Items.CountAsync(i => projectId == null || i.ProjectId == projectId),
      await db.Sprints.CountAsync(i => projectId == null || i.ProjectId == projectId),
      await db.SprintItems.CountAsync(i => projectId == null || i.Sprint.ProjectId == projectId),
      await db.ItemFieldValues.CountAsync(i => projectId == null || i.Item.ProjectId == projectId),
      await db.Templates.CountAsync(i => projectId == null || i.ProjectId == projectId),
      await db.TemplateItems.CountAsync(i => projectId == null || i.Section.Column.Table.Template.ProjectId == projectId),
      await db.WorkflowStates.CountAsync(i => projectId == null || i.ProjectId == projectId),
      await db.Roles.CountAsync(i => projectId == null || i.ProjectId == projectId),
      await db.Memberships.CountAsync(i => projectId == null || i.ProjectId == projectId),
      await db.ProjectInvitations.CountAsync(i => projectId == null || i.ProjectId == projectId),
      await db.Projects.CountAsync(i => projectId == null || i.Id == projectId)
    ));

  private async Task FillAsync(TestApi owner, int projectId)
  {
    var feature = await owner.CreateItemAsync(projectId, "F", (int)ItemType.Feature);
    var story = await owner.CreateItemAsync(projectId, "S", (int)ItemType.UserStory, feature);
    var task = await owner.CreateItemAsync(projectId, "T", (int)ItemType.Task, story);
    var sprint = await owner.CreateSprintAsync(projectId);
    (await owner.Post($"projects/sprints/{sprint}/items", new { itemId = task })).Code.ShouldBe(204);
    var attribute = await Factory.WithDbAsync(db => db.TemplateItems
      .Where(i => i.Section.Column.Table.Template.ProjectId == projectId && i.Section.Column.Table.Template.Type == ItemType.Task && (i.Type == TemplateItemType.SingleLineText || i.Type == TemplateItemType.MultiLineText))
      .Select(i => i.Id).FirstAsync());
    (await owner.Put($"projects/items/{task}/fields", new { values = new[] { new { templateItemId = attribute, value = "x" } } })).Code.ShouldBe(204);
    var roleId = await RoleId(projectId, RoleSystemKind.Member);
    (await owner.Post($"projects/{projectId}/invitations", new { roleId })).Code.ShouldBe(201);
    (await owner.Post($"projects/{projectId}/roles", new { title = "Custom", canViewProject = true, canManageProject = false, canViewMembers = true, canManageMembers = false, canManageSprints = false, canEditItems = false })).Code.ShouldBe(201);
  }

  [Fact]
  public async Task Delete_ByNonMember_Is404_ByGuestMemberManager_Is403_AndNothingIsDeleted()
  {
    var owner = await Api.RegisterAndLoginAsync();
    var projectId = await owner.CreateProjectAsync();

    (await (await Api.RegisterAndLoginAsync()).Delete($"projects/{projectId}")).Code.ShouldBe(404);
    foreach (var kind in new[] { RoleSystemKind.Guest, RoleSystemKind.Member, RoleSystemKind.Manager })
    {
      var user = await JoinAsync(owner, projectId, kind);
      (await user.Delete($"projects/{projectId}")).Code.ShouldBe(403, kind.ToString());
      (await user.Patch($"projects/{projectId}", new { title = "Renamed", description = "" })).Code.ShouldBe(403, kind.ToString());
    }

    (await CountAsync(projectId)).Projects.ShouldBe(1);
  }

  [Fact]
  public async Task Delete_ByOwner_RemovesEverythingOfTheProject_AndKeepsOthers()
  {
    var owner = await Api.RegisterAndLoginAsync();
    var doomed = await owner.CreateProjectAsync();
    var kept = await owner.CreateProjectAsync();
    await FillAsync(owner, doomed);
    await FillAsync(owner, kept);
    var keptBefore = await CountAsync(kept);
    var globalTemplates = await Factory.WithDbAsync(db => db.Templates.CountAsync(t => t.ProjectId == null));
    globalTemplates.ShouldBeGreaterThan(0);
    (await CountAsync(doomed)).Items.ShouldBe(3);

    (await owner.Delete($"projects/{doomed}")).Code.ShouldBe(204);

    (await CountAsync(doomed)).ShouldBe(new Counts(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));
    (await CountAsync(kept)).ShouldBe(keptBefore);
    (await Factory.WithDbAsync(db => db.Templates.CountAsync(t => t.ProjectId == null))).ShouldBe(globalTemplates);
    // no orphan rows anywhere
    (await Factory.WithDbAsync(db => db.TemplateItems.CountAsync())).ShouldBeGreaterThan(0);
  }

  [Fact]
  public async Task Delete_InvalidatesAccessAtOnce_AndUnknownProjectIs404()
  {
    var owner = await Api.RegisterAndLoginAsync();
    var projectId = await owner.CreateProjectAsync();
    (await owner.Get($"projects/{projectId}")).Code.ShouldBe(200); // warms the cache

    (await owner.Delete($"projects/{projectId}")).Code.ShouldBe(204);

    (await owner.Get($"projects/{projectId}")).Code.ShouldBe(404);
    (await owner.Delete($"projects/{projectId}")).Code.ShouldBe(404);
  }
}

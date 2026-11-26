using Eng.Agilium.Be.Model.Db;
using Eng.Agilium.Be.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Eng.Agilium.Be.Tests;

public class ItemsTests(ApiFixture fixture) : ApiTestBase(fixture)
{
  private const int Task = (int)ItemType.Task;
  private const int Bug = (int)ItemType.Bug;
  private const int Story = (int)ItemType.UserStory;
  private const int Feature = (int)ItemType.Feature;

  private async Task<(TestApi user, int project)> ArrangeAsync()
  {
    var user = await Api.RegisterAndLoginAsync();
    return (user, await user.CreateProjectAsync());
  }

  [Theory]
  [InlineData(Story, Task)] // story under a task
  [InlineData(Task, Feature)] // task directly under a feature
  [InlineData(Feature, Feature)] // feature never has a parent
  public async Task Create_InvalidHierarchy_Returns400(int childType, int parentType)
  {
    var (user, project) = await ArrangeAsync();
    var parent = await user.CreateItemAsync(project, "parent", parentType);

    var response = await user.Post("projects/items", new { title = "child", type = childType, projectId = project, parentId = parent });
    response.Code.ShouldBe(400);
  }

  [Fact]
  public async Task Create_ParentFromAnotherProject_Returns400()
  {
    var (user, project) = await ArrangeAsync();
    var other = await user.CreateProjectAsync();
    var foreignFeature = await user.CreateItemAsync(other, "F", Feature);

    var response = await user.Post("projects/items", new { title = "S", type = Story, projectId = project, parentId = foreignFeature });
    response.Code.ShouldBe(400);
  }

  [Fact]
  public async Task Create_OrphanTask_CreatesGenericContainersOnlyOncePerProject()
  {
    var (user, project) = await ArrangeAsync();

    await user.CreateItemAsync(project, "T1", Task);
    await user.CreateItemAsync(project, "T2", Bug);
    await user.CreateItemAsync(project, "S1", Story);

    await Factory.WithDbAsync(async db =>
    {
      var generics = await db.Items.Where(i => i.ProjectId == project && i.IsGeneric).ToListAsync();
      generics.Count(i => i.Type == ItemType.Feature).ShouldBe(1);
      generics.Count(i => i.Type == ItemType.UserStory).ShouldBe(1);
      var genericStory = generics.Single(i => i.Type == ItemType.UserStory);
      db.Items.Count(i => i.ProjectId == project && i.ParentId == genericStory.Id).ShouldBe(2);
    });
  }

  [Fact]
  public async Task GenericContainers_CannotBeRenamedMovedOrDeleted()
  {
    var (user, project) = await ArrangeAsync();
    await user.CreateItemAsync(project, "T", Task);
    var genericStory = await Factory.WithDbAsync(db =>
      db.Items.Where(i => i.ProjectId == project && i.IsGeneric && i.Type == ItemType.UserStory).Select(i => i.Id).SingleAsync()
    );
    var feature = await user.CreateItemAsync(project, "F", Feature);

    (await user.Patch($"projects/items/{genericStory}", new { title = "x", type = Story })).Code.ShouldBe(400);
    (await user.Patch($"projects/items/{genericStory}/parent", new { parentId = feature })).Code.ShouldBe(400);
    (await user.Delete($"projects/items/{genericStory}")).Code.ShouldBe(400);
  }

  [Fact]
  public async Task UpdateParent_UnderOwnDescendant_Returns400()
  {
    var (user, project) = await ArrangeAsync();
    var feature = await user.CreateItemAsync(project, "F", Feature);
    var feature2 = await user.CreateItemAsync(project, "F2", Feature);
    var story = await user.CreateItemAsync(project, "S", Story, feature);

    // moving the story under another feature is fine
    (await user.Patch($"projects/items/{story}/parent", new { parentId = feature2 })).Code.ShouldBe(204);
    // a feature can never get a parent (also covers the cycle case)
    (await user.Patch($"projects/items/{feature2}/parent", new { parentId = story })).Code.ShouldBe(400);
  }

  [Fact]
  public async Task Update_TaskToBug_Works_ButOtherTypeChangesReturn400()
  {
    var (user, project) = await ArrangeAsync();
    var task = await user.CreateItemAsync(project, "T", Task);

    (await user.Patch($"projects/items/{task}", new { title = "T2", type = Bug })).Code.ShouldBe(204);
    (await user.Patch($"projects/items/{task}", new { title = "T2", type = Story })).Code.ShouldBe(400);
  }

  [Fact]
  public async Task Update_TaskToBug_DeletesStoredFieldValues()
  {
    var (user, project) = await ArrangeAsync();
    var task = await user.CreateItemAsync(project, "T", Task);
    var fields = await user.Get($"projects/items/{task}/fields");
    var attribute = fields.Json.GetProperty("tables")[0].GetProperty("columns").EnumerateArray()
      .SelectMany(c => c.GetProperty("sections").EnumerateArray())
      .SelectMany(s => s.GetProperty("items").EnumerateArray())
      .First(a => a.GetProperty("type").GetInt32() == (int)TemplateItemType.Integer);
    var templateItemId = attribute.GetProperty("templateItemId").GetInt32();
    (await user.Put($"projects/items/{task}/fields", new { values = new[] { new { templateItemId, value = "5" } } })).Code.ShouldBe(204);
    (await Factory.WithDbAsync(db => db.ItemFieldValues.CountAsync(v => v.ItemId == task))).ShouldBe(1);

    (await user.Patch($"projects/items/{task}", new { title = "T", type = Bug })).Code.ShouldBe(204);

    (await Factory.WithDbAsync(db => db.ItemFieldValues.CountAsync(v => v.ItemId == task))).ShouldBe(0);
  }

  [Fact]
  public async Task UpdateAssignee_MustBeProjectMember()
  {
    var (user, project) = await ArrangeAsync();
    var outsider = await Api.RegisterAndLoginAsync();
    var task = await user.CreateItemAsync(project, "T", Task);

    (await user.Patch($"projects/items/{task}/assignee", new { assigneeId = outsider.UserId })).Code.ShouldBe(400);
    (await user.Patch($"projects/items/{task}/assignee", new { assigneeId = user.UserId })).Code.ShouldBe(204);
    (await user.Patch($"projects/items/{task}/assignee", new { assigneeId = (int?)null })).Code.ShouldBe(204);
  }

  [Fact]
  public async Task Delete_CascadesToDescendantsSprintItemsAndFieldValues()
  {
    var (user, project) = await ArrangeAsync();
    var feature = await user.CreateItemAsync(project, "F", Feature);
    var story = await user.CreateItemAsync(project, "S", Story, feature);
    var task = await user.CreateItemAsync(project, "T", Task, story);
    var sprint = await user.CreateSprintAsync(project);
    (await user.Post($"projects/sprints/{sprint}/items", new { itemId = task })).Code.ShouldBe(204);

    (await user.Delete($"projects/items/{feature}")).Code.ShouldBe(204);

    await Factory.WithDbAsync(async db =>
    {
      (await db.Items.CountAsync(i => i.ProjectId == project && !i.IsGeneric)).ShouldBe(0);
      (await db.SprintItems.CountAsync()).ShouldBe(0);
    });
  }

  [Fact]
  public async Task Create_UnknownProject_Returns404()
  {
    var user = await Api.RegisterAndLoginAsync();
    var response = await user.Post("projects/items", new { title = "T", type = Task, projectId = 99999, parentId = (int?)null });
    response.Code.ShouldBe(404);
  }
}

using Eng.Agilium.Be.Model.Db;
using Eng.Agilium.Be.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Eng.Agilium.Be.Tests;

public class CommentsTests(ApiFixture fixture) : ApiTestBase(fixture)
{
  private const int Task = (int)ItemType.Task;

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

  /// <summary>The Comments element of the project's Task template.</summary>
  private Task<int> CommentsElement(int projectId) =>
    Factory.WithDbAsync(db =>
      db.TemplateItems
        .Where(i =>
          i.Type == TemplateItemType.Comments
          && i.Section.Column.Table.Template.ProjectId == projectId
          && i.Section.Column.Table.Template.Type == ItemType.Task
        )
        .Select(i => i.Id)
        .SingleAsync()
    );

  private async Task<(TestApi Owner, int ProjectId, int TaskId, int Element)> ArrangeAsync()
  {
    var owner = await Api.RegisterAndLoginAsync();
    var projectId = await owner.CreateProjectAsync();
    var task = await owner.CreateItemAsync(projectId, "T", Task);
    return (owner, projectId, task, await CommentsElement(projectId));
  }

  private static async Task<int> AddAsync(TestApi user, int task, int element, string text = "Hello **world**")
  {
    var response = await user.Post($"projects/items/{task}/comments", new { templateItemId = element, text });
    response.Code.ShouldBe(201, response.Body);
    return response.Id;
  }

  private static async Task<System.Text.Json.JsonElement> ListAsync(TestApi user, int task, int element)
  {
    var response = await user.Get($"projects/items/{task}/comments?templateItemId={element}");
    response.Code.ShouldBe(200, response.Body);
    return response.Json.GetProperty("comments");
  }

  [Fact]
  public async Task Create_AndList_ReturnsTheCommentWithAuthorAndFlags()
  {
    var (owner, _, task, element) = await ArrangeAsync();

    var id = await AddAsync(owner, task, element, "  # Title\n\nText  ");

    var comments = await ListAsync(owner, task, element);
    comments.GetArrayLength().ShouldBe(1);
    comments[0].GetProperty("id").GetInt32().ShouldBe(id);
    comments[0].GetProperty("text").GetString().ShouldBe("# Title\n\nText"); // trimmed, Markdown kept as is
    comments[0].GetProperty("authorName").GetString().ShouldBe("Test User");
    comments[0].GetProperty("canEdit").GetBoolean().ShouldBeTrue();
    comments[0].GetProperty("canDelete").GetBoolean().ShouldBeTrue();
    comments[0].GetProperty("editedAt").ValueKind.ShouldBe(System.Text.Json.JsonValueKind.Null);
  }

  [Fact]
  public async Task List_IsOldestFirst()
  {
    var (owner, _, task, element) = await ArrangeAsync();
    await AddAsync(owner, task, element, "first");
    await AddAsync(owner, task, element, "second");

    var comments = await ListAsync(owner, task, element);

    comments.EnumerateArray().Select(c => c.GetProperty("text").GetString()).ShouldBe(["first", "second"]);
  }

  [Fact]
  public async Task NonMember_Gets404()
  {
    var (owner, _, task, element) = await ArrangeAsync();
    var comment = await AddAsync(owner, task, element);
    var stranger = await Api.RegisterAndLoginAsync();

    (await stranger.Get($"projects/items/{task}/comments?templateItemId={element}")).Code.ShouldBe(404);
    (await stranger.Post($"projects/items/{task}/comments", new { templateItemId = element, text = "x" })).Code.ShouldBe(404);
    (await stranger.Patch($"projects/items/comments/{comment}", new { text = "x" })).Code.ShouldBe(404);
    (await stranger.Delete($"projects/items/comments/{comment}")).Code.ShouldBe(404);
  }

  [Fact]
  public async Task Guest_CanRead_ButNotWrite()
  {
    var (owner, projectId, task, element) = await ArrangeAsync();
    var comment = await AddAsync(owner, task, element);
    var guest = await JoinAsync(owner, projectId, RoleSystemKind.Guest);

    (await ListAsync(guest, task, element)).GetArrayLength().ShouldBe(1);
    (await guest.Post($"projects/items/{task}/comments", new { templateItemId = element, text = "x" })).Code.ShouldBe(403);
    (await guest.Patch($"projects/items/comments/{comment}", new { text = "x" })).Code.ShouldBe(403);
    (await guest.Delete($"projects/items/comments/{comment}")).Code.ShouldBe(403);
  }

  [Fact]
  public async Task Member_WritesAndEditsOwnComments_ButNotOthers()
  {
    var (owner, projectId, task, element) = await ArrangeAsync();
    var ownersComment = await AddAsync(owner, task, element, "owner");
    var member = await JoinAsync(owner, projectId, RoleSystemKind.Member);
    var mine = await AddAsync(member, task, element, "member");

    (await member.Patch($"projects/items/comments/{mine}", new { text = "member edited" })).Code.ShouldBe(204);
    (await member.Patch($"projects/items/comments/{ownersComment}", new { text = "hijack" })).Code.ShouldBe(403);
    (await member.Delete($"projects/items/comments/{ownersComment}")).Code.ShouldBe(403);

    var comments = await ListAsync(member, task, element);
    var ownersRow = comments.EnumerateArray().Single(c => c.GetProperty("id").GetInt32() == ownersComment);
    ownersRow.GetProperty("canEdit").GetBoolean().ShouldBeFalse();
    ownersRow.GetProperty("canDelete").GetBoolean().ShouldBeFalse();
    var mineRow = comments.EnumerateArray().Single(c => c.GetProperty("id").GetInt32() == mine);
    mineRow.GetProperty("text").GetString().ShouldBe("member edited");
    mineRow.GetProperty("editedAt").ValueKind.ShouldNotBe(System.Text.Json.JsonValueKind.Null);
    mineRow.GetProperty("canEdit").GetBoolean().ShouldBeTrue();

    (await member.Delete($"projects/items/comments/{mine}")).Code.ShouldBe(204);
  }

  [Fact]
  public async Task OwnerAndManager_CanDeleteSomeoneElsesComment_ButNotEditIt()
  {
    var (owner, projectId, task, element) = await ArrangeAsync();
    var member = await JoinAsync(owner, projectId, RoleSystemKind.Member);
    var manager = await JoinAsync(owner, projectId, RoleSystemKind.Manager);
    var first = await AddAsync(member, task, element, "a");
    var second = await AddAsync(member, task, element, "b");

    (await owner.Patch($"projects/items/comments/{first}", new { text = "x" })).Code.ShouldBe(403);
    (await manager.Delete($"projects/items/comments/{first}")).Code.ShouldBe(204);
    (await owner.Delete($"projects/items/comments/{second}")).Code.ShouldBe(204);

    (await ListAsync(owner, task, element)).GetArrayLength().ShouldBe(0);
  }

  [Theory]
  [InlineData("")]
  [InlineData("   \n ")]
  public async Task Create_BlankText_Is400(string text)
  {
    var (owner, _, task, element) = await ArrangeAsync();

    (await owner.Post($"projects/items/{task}/comments", new { templateItemId = element, text })).Code.ShouldBe(400);
  }

  [Fact]
  public async Task Create_TooLongText_Is400_AndTheLimitItselfPasses()
  {
    var (owner, _, task, element) = await ArrangeAsync();

    (await owner.Post($"projects/items/{task}/comments", new { templateItemId = element, text = new string('a', ItemComment.MaxTextLength + 1) })).Code.ShouldBe(400);
    (await owner.Post($"projects/items/{task}/comments", new { templateItemId = element, text = new string('a', ItemComment.MaxTextLength) })).Code.ShouldBe(201);
  }

  [Fact]
  public async Task Create_ForAnElementThatIsNotACommentsElementOfThisTemplate_Is400()
  {
    var (owner, projectId, task, _) = await ArrangeAsync();
    var otherProject = await owner.CreateProjectAsync();
    var description = await Factory.WithDbAsync(db =>
      db.TemplateItems.Where(i => i.Key == "description" && i.Section.Column.Table.Template.ProjectId == projectId && i.Section.Column.Table.Template.Type == ItemType.Task).Select(i => i.Id).SingleAsync());
    var foreign = await CommentsElement(otherProject);

    (await owner.Post($"projects/items/{task}/comments", new { templateItemId = description, text = "x" })).Code.ShouldBe(400);
    (await owner.Post($"projects/items/{task}/comments", new { templateItemId = foreign, text = "x" })).Code.ShouldBe(400);
    (await owner.Post($"projects/items/{task}/comments", new { templateItemId = 999999, text = "x" })).Code.ShouldBe(400);
  }

  [Fact]
  public async Task CommentsOfDifferentItems_AreSeparate()
  {
    var (owner, projectId, task, element) = await ArrangeAsync();
    var otherTask = await owner.CreateItemAsync(projectId, "T2", Task);
    await AddAsync(owner, task, element);

    (await ListAsync(owner, otherTask, element)).GetArrayLength().ShouldBe(0);
  }

  [Fact]
  public async Task SetFieldValues_RejectsAValueForACommentsElement()
  {
    var (owner, _, task, element) = await ArrangeAsync();

    var response = await owner.Put($"projects/items/{task}/fields", new { values = new[] { new { templateItemId = element, value = "text" } } });

    response.Code.ShouldBe(400);
  }

  [Fact]
  public async Task DeletingTheItem_DeletesItsComments()
  {
    var (owner, projectId, task, element) = await ArrangeAsync();
    await AddAsync(owner, task, element);

    (await owner.Delete($"projects/items/{task}")).Code.ShouldBe(204);

    (await Factory.WithDbAsync(db => db.ItemComments.CountAsync(c => c.Item.ProjectId == projectId))).ShouldBe(0);
  }

  [Fact]
  public async Task DeletingTheProject_DeletesItsComments_AndKeepsOthers()
  {
    var (owner, projectId, task, element) = await ArrangeAsync();
    await AddAsync(owner, task, element);
    var other = await owner.CreateProjectAsync();
    var otherTask = await owner.CreateItemAsync(other, "T", Task);
    await AddAsync(owner, otherTask, await CommentsElement(other));

    (await owner.Delete($"projects/{projectId}")).Code.ShouldBe(204);

    (await Factory.WithDbAsync(db => db.ItemComments.CountAsync())).ShouldBe(1);
  }
}

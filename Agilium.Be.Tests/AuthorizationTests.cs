using Eng.Agilium.Be.Model.Db;
using Eng.Agilium.Be.Services;
using Eng.Agilium.Be.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Eng.Agilium.Be.Tests;

public class AuthorizationTests(ApiFixture fixture) : ApiTestBase(fixture)
{
  /// <summary>Adds a new user to the project with the system role directly in the database (invitations come in Step 3).</summary>
  private async Task<TestApi> AddMemberAsync(int projectId, RoleSystemKind kind)
  {
    var user = await Api.RegisterAndLoginAsync();
    await Factory.WithDbAsync(async db =>
    {
      var role = await db.Roles.SingleAsync(r => r.ProjectId == projectId && r.SystemKind == kind);
      db.Memberships.Add(new Membership { ProjectId = projectId, UserId = user.UserId, RoleId = role.Id });
      await db.SaveChangesAsync();
    });
    return user;
  }

  [Fact]
  public async Task ProtectedEndpoint_WithoutToken_Is401()
  {
    var owner = await Api.RegisterAndLoginAsync();
    var projectId = await owner.CreateProjectAsync();

    (await Api.Get("projects")).Code.ShouldBe(401);
    (await Api.Get($"projects/{projectId}")).Code.ShouldBe(401);
    (await Api.Post("projects", new { title = "x", description = "" })).Code.ShouldBe(401);
  }

  [Fact]
  public async Task NonMember_GetsNotFound_NotForbidden()
  {
    var owner = await Api.RegisterAndLoginAsync();
    var projectId = await owner.CreateProjectAsync();
    var stranger = await Api.RegisterAndLoginAsync();

    (await stranger.Get($"projects/{projectId}")).Code.ShouldBe(404);
    (await stranger.Get($"projects/{projectId}/items")).Code.ShouldBe(404);
    (await stranger.Get($"projects/{projectId}/sprints")).Code.ShouldBe(404);
    (await stranger.Get("projects/999999")).Code.ShouldBe(404);
  }

  [Fact]
  public async Task List_ReturnsOnlyOwnProjects()
  {
    var a = await Api.RegisterAndLoginAsync();
    var b = await Api.RegisterAndLoginAsync();
    var pa = await a.CreateProjectAsync("A-project");
    await b.CreateProjectAsync("B-project");

    var list = (await a.Get("projects")).Json.GetProperty("projects");
    list.GetArrayLength().ShouldBe(1);
    list[0].GetProperty("id").GetInt32().ShouldBe(pa);
  }

  [Fact]
  public async Task Get_ReturnsPermissionsOfTheCaller()
  {
    var owner = await Api.RegisterAndLoginAsync();
    var projectId = await owner.CreateProjectAsync();
    var guest = await AddMemberAsync(projectId, RoleSystemKind.Guest);

    var o = (await owner.Get($"projects/{projectId}")).Json.GetProperty("permissions");
    o.GetProperty("isOwner").GetBoolean().ShouldBeTrue();
    o.GetProperty("canManageProject").GetBoolean().ShouldBeTrue();

    var g = (await guest.Get($"projects/{projectId}")).Json.GetProperty("permissions");
    g.GetProperty("isOwner").GetBoolean().ShouldBeFalse();
    g.GetProperty("canViewProject").GetBoolean().ShouldBeTrue();
    g.GetProperty("canEditItems").GetBoolean().ShouldBeFalse();
  }

  [Fact]
  public async Task Guest_CanRead_ButNotWrite()
  {
    var owner = await Api.RegisterAndLoginAsync();
    var projectId = await owner.CreateProjectAsync();
    var itemId = await owner.CreateItemAsync(projectId, "Feature", 1);
    var guest = await AddMemberAsync(projectId, RoleSystemKind.Guest);

    (await guest.Get($"projects/{projectId}/items")).Code.ShouldBe(200);
    (await guest.Post("projects/items", new { title = "x", type = 1, projectId })).Code.ShouldBe(403);
    (await guest.Delete($"projects/items/{itemId}")).Code.ShouldBe(403);
    (await guest.Post("projects/sprints", new { projectId, title = "S" })).Code.ShouldBe(403);
  }

  [Fact]
  public async Task Member_CanEditItems_ButNotManageSprintsOrProject()
  {
    var owner = await Api.RegisterAndLoginAsync();
    var projectId = await owner.CreateProjectAsync();
    var member = await AddMemberAsync(projectId, RoleSystemKind.Member);

    (await member.Post("projects/items", new { title = "x", type = 1, projectId })).Code.ShouldBe(201);
    (await member.Post("projects/sprints", new { projectId, title = "S" })).Code.ShouldBe(403);
    (await member.Patch($"projects/{projectId}/state", new { status = 2 })).Code.ShouldBe(403);
  }

  [Fact]
  public async Task Manager_CanManageProject_ButNotRenameIt_OnlyOwnerCan()
  {
    var owner = await Api.RegisterAndLoginAsync();
    var projectId = await owner.CreateProjectAsync("Original");
    var manager = await AddMemberAsync(projectId, RoleSystemKind.Manager);

    (await manager.Post("projects/sprints", new { projectId, title = "S" })).Code.ShouldBe(201);
    (await manager.Patch($"projects/{projectId}", new { title = "Renamed", description = "" })).Code.ShouldBe(403);
    (await owner.Patch($"projects/{projectId}", new { title = "Renamed", description = "" })).Code.ShouldBe(204);
  }

  [Fact]
  public async Task ItemOfAnotherProject_IsNotReachable_ByMemberOfOtherProject()
  {
    var a = await Api.RegisterAndLoginAsync();
    var b = await Api.RegisterAndLoginAsync();
    var pa = await a.CreateProjectAsync();
    await b.CreateProjectAsync();
    var item = await a.CreateItemAsync(pa, "Secret", 1);

    (await b.Get($"projects/items/{item}/fields")).Code.ShouldBe(404);
    (await b.Delete($"projects/items/{item}")).Code.ShouldBe(404);
  }

  [Fact]
  public async Task RemovedMember_LosesAccessImmediately_DespiteCache()
  {
    var owner = await Api.RegisterAndLoginAsync();
    var projectId = await owner.CreateProjectAsync();
    var member = await AddMemberAsync(projectId, RoleSystemKind.Member);

    (await member.Get($"projects/{projectId}")).Code.ShouldBe(200); // fills the cache

    var membershipId = await Factory.WithDbAsync(db =>
      db.Memberships.Where(m => m.ProjectId == projectId && m.UserId == member.UserId).Select(m => m.Id).SingleAsync()
    );
    (await owner.Delete($"projects/membership/{membershipId}")).Code.ShouldBe(204);

    (await member.Get($"projects/{projectId}")).Code.ShouldBe(404);
  }

  [Fact]
  public async Task LastOwner_CannotBeRemoved()
  {
    var owner = await Api.RegisterAndLoginAsync();
    var projectId = await owner.CreateProjectAsync();
    var membershipId = await Factory.WithDbAsync(db =>
      db.Memberships.Where(m => m.ProjectId == projectId && m.UserId == owner.UserId).Select(m => m.Id).SingleAsync()
    );

    (await owner.Delete($"projects/membership/{membershipId}")).Code.ShouldBe(400);
  }

  [Fact]
  public async Task DeactivatedUser_WithValidToken_Is401()
  {
    var user = await Api.RegisterAndLoginAsync();
    (await user.Get("projects")).Code.ShouldBe(200);

    await Factory.WithDbAsync(async db =>
    {
      (await db.AppUsers.SingleAsync(u => u.Id == user.UserId)).IsActive = false;
      await db.SaveChangesAsync();
    });
    Factory.Services.GetRequiredService<IAccessCache>().InvalidateUser(user.UserId);

    (await user.Get("projects")).Code.ShouldBe(401);
  }
}

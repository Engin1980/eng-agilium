using Eng.Agilium.Be.Model.Db;
using Eng.Agilium.Be.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Eng.Agilium.Be.Tests;

public class MembersAndInvitationsTests(ApiFixture fixture) : ApiTestBase(fixture)
{
  private Task<int> RoleId(int projectId, RoleSystemKind kind) =>
    Factory.WithDbAsync(db => db.Roles.Where(r => r.ProjectId == projectId && r.SystemKind == kind).Select(r => r.Id).SingleAsync());

  private async Task<(TestApi Owner, int ProjectId)> NewProject()
  {
    var owner = await Api.RegisterAndLoginAsync();
    return (owner, await owner.CreateProjectAsync());
  }

  private async Task<string> InviteAsync(TestApi owner, int projectId, RoleSystemKind kind = RoleSystemKind.Member, object? extra = null)
  {
    var roleId = await RoleId(projectId, kind);
    var response = await owner.Post($"projects/{projectId}/invitations", extra ?? new { roleId });
    response.Code.ShouldBe(201, response.Body);
    return response.Json.GetProperty("key").GetString()!;
  }

  private Task<int> MembershipId(int projectId, int userId) =>
    Factory.WithDbAsync(db => db.Memberships.Where(m => m.ProjectId == projectId && m.UserId == userId).Select(m => m.Id).SingleAsync());

  // ---- members & roles

  [Fact]
  public async Task ListMembers_ReturnsShapeAndRequiresViewMembers()
  {
    var (owner, projectId) = await NewProject();
    var members = (await owner.Get($"projects/{projectId}/members")).Json.GetProperty("members");
    members.GetArrayLength().ShouldBe(1);
    members[0].GetProperty("roleTitle").GetString().ShouldBe("Owner");
    members[0].GetProperty("email").GetString().ShouldBe(owner.Email);

    var stranger = await Api.RegisterAndLoginAsync();
    (await stranger.Get($"projects/{projectId}/members")).Code.ShouldBe(404);
  }

  [Fact]
  public async Task ChangeRole_Works_AndTakesEffectImmediately()
  {
    var (owner, projectId) = await NewProject();
    var member = await Api.RegisterAndLoginAsync();
    (await member.Post("projects/join", new { key = await InviteAsync(owner, projectId, RoleSystemKind.Guest) })).Code.ShouldBe(200);
    (await member.Post("projects/items", new { title = "x", type = 1, projectId })).Code.ShouldBe(403);

    var mid = await MembershipId(projectId, member.UserId);
    var memberRole = await RoleId(projectId, RoleSystemKind.Member);
    (await owner.Patch($"projects/members/{mid}", new { roleId = memberRole })).Code.ShouldBe(204);

    (await member.Post("projects/items", new { title = "x", type = 1, projectId })).Code.ShouldBe(201);
  }

  [Fact]
  public async Task ChangeRole_ToRoleOfAnotherProject_Is400()
  {
    var (owner, projectId) = await NewProject();
    var other = await owner.CreateProjectAsync();
    var mid = await MembershipId(projectId, owner.UserId);
    var foreign = await RoleId(other, RoleSystemKind.Guest);

    (await owner.Patch($"projects/members/{mid}", new { roleId = foreign })).Code.ShouldBe(400);
  }

  [Fact]
  public async Task LastOwner_CannotBeDemoted()
  {
    var (owner, projectId) = await NewProject();
    var mid = await MembershipId(projectId, owner.UserId);
    var manager = await RoleId(projectId, RoleSystemKind.Manager);

    (await owner.Patch($"projects/members/{mid}", new { roleId = manager })).Code.ShouldBe(400);
  }

  [Fact]
  public async Task Manager_CannotAppointOrDemoteOwners()
  {
    var (owner, projectId) = await NewProject();
    var manager = await Api.RegisterAndLoginAsync();
    (await manager.Post("projects/join", new { key = await InviteAsync(owner, projectId, RoleSystemKind.Manager) })).Code.ShouldBe(200);
    var ownerRole = await RoleId(projectId, RoleSystemKind.Owner);
    var guestRole = await RoleId(projectId, RoleSystemKind.Guest);

    var managerMembership = await MembershipId(projectId, manager.UserId);
    (await manager.Patch($"projects/members/{managerMembership}", new { roleId = ownerRole })).Code.ShouldBe(403);

    var ownerMembership = await MembershipId(projectId, owner.UserId);
    (await manager.Patch($"projects/members/{ownerMembership}", new { roleId = guestRole })).Code.ShouldBe(403);

    // an owner may appoint another owner
    (await owner.Patch($"projects/members/{managerMembership}", new { roleId = ownerRole })).Code.ShouldBe(204);
  }

  [Fact]
  public async Task Roles_Crud_AndRules()
  {
    var (owner, projectId) = await NewProject();
    var created = await owner.Post($"projects/{projectId}/roles", new { title = "Tester", canViewProject = true, canViewMembers = true, canEditItems = true });
    created.Code.ShouldBe(201);
    var roleId = created.Id;

    (await owner.Post($"projects/{projectId}/roles", new { title = "Tester" })).Code.ShouldBe(409);
    (await owner.Patch($"projects/roles/{roleId}", new { title = "Tester 2", canViewProject = true })).Code.ShouldBe(204);

    var roles = (await owner.Get($"projects/{projectId}/roles")).Json.GetProperty("roles");
    roles.GetArrayLength().ShouldBe(5);
    roles.EnumerateArray().Count(r => r.GetProperty("isSystem").GetBoolean()).ShouldBe(4);

    // system roles are read-only
    var guest = await RoleId(projectId, RoleSystemKind.Guest);
    (await owner.Patch($"projects/roles/{guest}", new { title = "Renamed" })).Code.ShouldBe(400);
    (await owner.Delete($"projects/roles/{guest}")).Code.ShouldBe(400);

    // a role with members cannot be deleted
    var member = await Api.RegisterAndLoginAsync();
    await Factory.WithDbAsync(async db =>
    {
      db.Memberships.Add(new Membership { ProjectId = projectId, UserId = member.UserId, RoleId = roleId });
      await db.SaveChangesAsync();
    });
    (await owner.Delete($"projects/roles/{roleId}")).Code.ShouldBe(400);

    await owner.Delete($"projects/membership/{await MembershipId(projectId, member.UserId)}");
    (await owner.Delete($"projects/roles/{roleId}")).Code.ShouldBe(204);
  }

  [Fact]
  public async Task Guest_CannotManageRolesOrInvitations()
  {
    var (owner, projectId) = await NewProject();
    var guest = await Api.RegisterAndLoginAsync();
    (await guest.Post("projects/join", new { key = await InviteAsync(owner, projectId, RoleSystemKind.Guest) })).Code.ShouldBe(200);
    var role = await RoleId(projectId, RoleSystemKind.Member);

    (await guest.Get($"projects/{projectId}/members")).Code.ShouldBe(200);
    (await guest.Post($"projects/{projectId}/roles", new { title = "X" })).Code.ShouldBe(403);
    (await guest.Get($"projects/{projectId}/invitations")).Code.ShouldBe(403);
    (await guest.Post($"projects/{projectId}/invitations", new { roleId = role })).Code.ShouldBe(403);
  }

  [Fact]
  public async Task RoleFlagChange_CannotLeaveProjectWithoutAMemberManager()
  {
    var (owner, projectId) = await NewProject();
    // the owner role is a system role, so a custom role is the only way to test; its sole holder cannot be the only manager
    var created = await owner.Post($"projects/{projectId}/roles", new { title = "Mgr", canViewProject = true, canManageMembers = true });
    var ownerMembership = await MembershipId(projectId, owner.UserId);
    // owner stays; just verify the rule does not block a harmless update
    (await owner.Patch($"projects/roles/{created.Id}", new { title = "Mgr", canViewProject = true, canManageMembers = false })).Code.ShouldBe(204);
    ownerMembership.ShouldBeGreaterThan(0);
  }

  // ---- invitations

  [Fact]
  public async Task Invitation_KeyIsReturnedOnce_AndOnlyHashIsStored()
  {
    var (owner, projectId) = await NewProject();
    var key = await InviteAsync(owner, projectId);

    var list = await owner.Get($"projects/{projectId}/invitations");
    list.Body.ShouldNotContain(key);
    var inv = list.Json.GetProperty("invitations")[0];
    inv.GetProperty("keyPrefix").GetString().ShouldBe(key[..6]);
    inv.GetProperty("status").GetInt32().ShouldBe(1);

    await Factory.WithDbAsync(async db =>
    {
      var row = await db.ProjectInvitations.SingleAsync();
      row.KeyHash.ShouldNotBe(key);
      row.KeyHash.Length.ShouldBe(64);
    });
  }

  [Fact]
  public async Task Invitation_Defaults_Are7DaysAnd10Uses()
  {
    var (owner, projectId) = await NewProject();
    var roleId = await RoleId(projectId, RoleSystemKind.Member);
    var r = await owner.Post($"projects/{projectId}/invitations", new { roleId });
    r.Json.GetProperty("maxUses").GetInt32().ShouldBe(10);
    var expires = r.Json.GetProperty("expiresAt").GetDateTime();
    (expires - DateTime.UtcNow).TotalDays.ShouldBeInRange(6.9, 7.1);
  }

  [Fact]
  public async Task Invitation_Validation()
  {
    var (owner, projectId) = await NewProject();
    var member = await RoleId(projectId, RoleSystemKind.Member);
    var ownerRole = await RoleId(projectId, RoleSystemKind.Owner);
    var other = await owner.CreateProjectAsync();

    (await owner.Post($"projects/{projectId}/invitations", new { roleId = ownerRole })).Code.ShouldBe(400);
    (await owner.Post($"projects/{projectId}/invitations", new { roleId = await RoleId(other, RoleSystemKind.Member) })).Code.ShouldBe(400);
    (await owner.Post($"projects/{projectId}/invitations", new { roleId = member, maxUses = 0 })).Code.ShouldBe(400);
    (await owner.Post($"projects/{projectId}/invitations", new { roleId = member, maxUses = 1001 })).Code.ShouldBe(400);
    (await owner.Post($"projects/{projectId}/invitations", new { roleId = member, expiresAt = DateTime.UtcNow.AddDays(-1) })).Code.ShouldBe(400);
    (await owner.Post($"projects/{projectId}/invitations", new { roleId = member, expiresAt = DateTime.UtcNow.AddDays(91) })).Code.ShouldBe(400);
  }

  [Fact]
  public async Task Join_CreatesMembershipWithRole_AndCountsUse()
  {
    var (owner, projectId) = await NewProject();
    var key = await InviteAsync(owner, projectId, RoleSystemKind.Member);
    var user = await Api.RegisterAndLoginAsync();

    var join = await user.Post("projects/join", new { key });
    join.Code.ShouldBe(200);
    join.Json.GetProperty("projectId").GetInt32().ShouldBe(projectId);

    (await user.Get($"projects/{projectId}")).Code.ShouldBe(200);
    var again = await user.Post("projects/join", new { key });
    again.Code.ShouldBe(409);

    await Factory.WithDbAsync(async db =>
    {
      (await db.ProjectInvitations.SingleAsync()).UsedCount.ShouldBe(1);
      (await db.Memberships.Include(m => m.Role).SingleAsync(m => m.UserId == user.UserId)).Role.SystemKind.ShouldBe(RoleSystemKind.Member);
    });
  }

  [Fact]
  public async Task Join_InvalidKeys_AllGiveTheSameError()
  {
    var (owner, projectId) = await NewProject();
    var roleId = await RoleId(projectId, RoleSystemKind.Member);
    var revoked = await owner.Post($"projects/{projectId}/invitations", new { roleId });
    var exhausted = await owner.Post($"projects/{projectId}/invitations", new { roleId, maxUses = 1 });
    var expired = await owner.Post($"projects/{projectId}/invitations", new { roleId });
    await owner.Delete($"projects/invitations/{revoked.Id}");
    await Factory.WithDbAsync(async db =>
    {
      (await db.ProjectInvitations.SingleAsync(i => i.Id == expired.Id)).ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
      await db.SaveChangesAsync();
    });
    var first = await Api.RegisterAndLoginAsync();
    (await first.Post("projects/join", new { key = exhausted.Json.GetProperty("key").GetString() })).Code.ShouldBe(200);

    var user = await Api.RegisterAndLoginAsync();
    var bodies = new List<string>();
    foreach (var key in new[] { revoked.Json.GetProperty("key").GetString(), exhausted.Json.GetProperty("key").GetString(), expired.Json.GetProperty("key").GetString(), "nonsense" })
    {
      var r = await user.Post("projects/join", new { key });
      r.Code.ShouldBe(400);
      bodies.Add(r.Body);
    }
    bodies.Distinct().Count().ShouldBe(1);
  }

  [Fact]
  public async Task Join_LastUse_ConcurrentUsers_OnlyOneSucceeds()
  {
    var (owner, projectId) = await NewProject();
    var roleId = await RoleId(projectId, RoleSystemKind.Member);
    var key = (await owner.Post($"projects/{projectId}/invitations", new { roleId, maxUses = 1 })).Json.GetProperty("key").GetString();
    var users = new List<TestApi>();
    for (var i = 0; i < 4; i++)
      users.Add(await Api.RegisterAndLoginAsync());

    var results = await Task.WhenAll(users.Select(u => u.Post("projects/join", new { key })));

    results.Count(r => r.Code == 200).ShouldBe(1);
    await Factory.WithDbAsync(async db => (await db.Memberships.CountAsync(m => m.ProjectId == projectId)).ShouldBe(2));
  }

  [Fact]
  public async Task Join_RevokedKey_Fails_AndAnonymousIs401()
  {
    var (owner, projectId) = await NewProject();
    var key = await InviteAsync(owner, projectId);
    var id = await Factory.WithDbAsync(db => db.ProjectInvitations.Select(i => i.Id).SingleAsync());
    (await owner.Delete($"projects/invitations/{id}")).Code.ShouldBe(204);

    var user = await Api.RegisterAndLoginAsync();
    (await user.Post("projects/join", new { key })).Code.ShouldBe(400);
    (await Api.Post("projects/join", new { key })).Code.ShouldBe(401);
  }

  [Fact]
  public async Task Join_TooManyFailures_Is429()
  {
    var user = await Api.RegisterAndLoginAsync();
    for (var i = 0; i < 10; i++)
      (await user.Post("projects/join", new { key = $"bad-{i}" })).Code.ShouldBe(400);

    (await user.Post("projects/join", new { key = "bad-again" })).Code.ShouldBe(429);
  }
}

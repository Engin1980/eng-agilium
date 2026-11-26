using System.Collections.Concurrent;
using Eng.Agilium.Be.Model.Db;

namespace Eng.Agilium.Be.Services;

public enum ProjectPermission
{
  ViewProject,
  ManageProject,
  ViewMembers,
  ManageMembers,
  ManageSprints,
  EditItems,
}

/// <summary>What a user may do in one project (a snapshot of the membership + role from the database).</summary>
public sealed record ProjectAccessEntry(
  bool IsMember,
  int RoleId,
  RoleSystemKind SystemKind,
  bool CanViewProject,
  bool CanManageProject,
  bool CanViewMembers,
  bool CanManageMembers,
  bool CanManageSprints,
  bool CanEditItems
)
{
  public static readonly ProjectAccessEntry NotMember = new(false, 0, RoleSystemKind.None, false, false, false, false, false, false);

  /// <summary>Owner-only rights (renaming/deleting the project, appointing owners) follow the system role, not a flag.</summary>
  public bool IsOwner => IsMember && SystemKind == RoleSystemKind.Owner;

  public bool Has(ProjectPermission permission) =>
    IsMember
    && permission switch
    {
      ProjectPermission.ViewProject => CanViewProject,
      ProjectPermission.ManageProject => CanManageProject,
      ProjectPermission.ViewMembers => CanViewMembers,
      ProjectPermission.ManageMembers => CanManageMembers,
      ProjectPermission.ManageSprints => CanManageSprints,
      ProjectPermission.EditItems => CanEditItems,
      _ => false,
    };

  public static ProjectAccessEntry From(Role role) =>
    new(
      true,
      role.Id,
      role.SystemKind,
      role.CanViewProject,
      role.CanManageProject,
      role.CanViewMembers,
      role.CanManageMembers,
      role.CanManageSprints,
      role.CanEditItems
    );

  /// <summary>Everything allowed - only for trusted internal callers such as the development seed.</summary>
  public static readonly ProjectAccessEntry Everything = new(true, 0, RoleSystemKind.Owner, true, true, true, true, true, true);
}

/// <summary>The account state checked on every authenticated request.</summary>
public sealed record UserStatus(bool Exists, bool IsActive)
{
  public bool CanUseApi => Exists && IsActive;
}

/// <summary>
/// Server-side cache of permissions and account state, so they are not read from the database on every request.
/// Entries are valid for <see cref="Ttl"/> at most (a safety net for changes made outside the API) and are
/// dropped explicitly by the code that changes memberships, roles or accounts. After a restart the cache is
/// empty and fills lazily from the database.
/// <para>
/// Invalidation bumps a version number that is part of the cache key, so a reader that loaded data from the
/// database before the change but stores it afterwards stores it under an outdated key and never serves it.
/// </para>
/// </summary>
public interface IAccessCache
{
  TimeSpan Ttl { get; }

  /// <summary>Returns the cached entry; <paramref name="version"/> must be passed to <see cref="SetAccess"/> after a database load.</summary>
  bool TryGetAccess(int userId, int projectId, out ProjectAccessEntry? entry, out long version);

  void SetAccess(int userId, int projectId, long version, ProjectAccessEntry entry);

  bool TryGetUser(int userId, out UserStatus? status, out long version);

  void SetUser(int userId, long version, UserStatus status);

  /// <summary>A membership or role of the project changed (any member, any role).</summary>
  void InvalidateProject(int projectId);

  /// <summary>The account state of the user changed.</summary>
  void InvalidateUser(int userId);

  /// <summary>Drops everything (what a server restart does).</summary>
  void Clear();
}

public sealed class AccessCache(TimeProvider clock) : IAccessCache
{
  private sealed record Cached<T>(T Value, DateTimeOffset ExpiresAt);

  private readonly ConcurrentDictionary<(int User, int Project, long Version), Cached<ProjectAccessEntry>> access = new();
  private readonly ConcurrentDictionary<(int User, long Version), Cached<UserStatus>> users = new();
  private readonly ConcurrentDictionary<int, long> projectVersions = new();
  private readonly ConcurrentDictionary<int, long> userVersions = new();
  private int writes;

  public TimeSpan Ttl { get; } = TimeSpan.FromMinutes(5);

  public bool TryGetAccess(int userId, int projectId, out ProjectAccessEntry? entry, out long version)
  {
    version = projectVersions.GetValueOrDefault(projectId);
    if (access.TryGetValue((userId, projectId, version), out var cached) && cached.ExpiresAt > clock.GetUtcNow())
    {
      entry = cached.Value;
      return true;
    }
    entry = null;
    return false;
  }

  public void SetAccess(int userId, int projectId, long version, ProjectAccessEntry entry)
  {
    if (projectVersions.GetValueOrDefault(projectId) != version)
      return; // something changed while the caller was reading the database - do not keep possibly stale data
    access[(userId, projectId, version)] = new Cached<ProjectAccessEntry>(entry, clock.GetUtcNow() + Ttl);
    SweepSometimes();
  }

  public bool TryGetUser(int userId, out UserStatus? status, out long version)
  {
    version = userVersions.GetValueOrDefault(userId);
    if (users.TryGetValue((userId, version), out var cached) && cached.ExpiresAt > clock.GetUtcNow())
    {
      status = cached.Value;
      return true;
    }
    status = null;
    return false;
  }

  public void SetUser(int userId, long version, UserStatus status)
  {
    if (userVersions.GetValueOrDefault(userId) != version)
      return;
    users[(userId, version)] = new Cached<UserStatus>(status, clock.GetUtcNow() + Ttl);
    SweepSometimes();
  }

  public void InvalidateProject(int projectId) => projectVersions.AddOrUpdate(projectId, 1, (_, v) => v + 1);

  public void InvalidateUser(int userId) => userVersions.AddOrUpdate(userId, 1, (_, v) => v + 1);

  public void Clear()
  {
    access.Clear();
    users.Clear();
    // versions are kept on purpose: they only grow, so an in-flight read cannot resurrect an old entry
  }

  /// <summary>Removes expired entries now and then (outdated versions expire the same way).</summary>
  private void SweepSometimes()
  {
    if (Interlocked.Increment(ref writes) % 500 != 0)
      return;
    var now = clock.GetUtcNow();
    foreach (var pair in access.Where(p => p.Value.ExpiresAt <= now))
      access.TryRemove(pair.Key, out _);
    foreach (var pair in users.Where(p => p.Value.ExpiresAt <= now))
      users.TryRemove(pair.Key, out _);
  }
}

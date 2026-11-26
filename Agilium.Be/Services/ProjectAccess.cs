using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Features;
using Eng.Agilium.Be.Model.Db;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Services;

/// <summary>
/// Authorization of the current request. Permissions come from <see cref="IAccessCache"/> (filled from the
/// database on a miss), so a change made through the API takes effect on the very next request.
/// A non-member (or a missing project) gets <see cref="EntityNotFoundException"/> (404, existence is not
/// revealed); a member without the permission gets <see cref="AuthorizationFailedException"/> (403).
/// </summary>
public interface IProjectAccess
{
  /// <summary>Sets the authenticated caller (done by the endpoint).</summary>
  void Bind(LoggedUser? user);

  /// <summary>Throws <see cref="AuthenticationFailedException"/> (401) unless the caller's account may use the API.</summary>
  Task EnsureActiveUserAsync(CancellationToken cancellationToken);

  /// <summary>The caller's access to the project; throws 404 for a non-member.</summary>
  Task<ProjectAccessEntry> GetAsync(int projectId, CancellationToken cancellationToken);

  Task<ProjectAccessEntry> EnsureCanAsync(int projectId, ProjectPermission permission, CancellationToken cancellationToken);

  /// <summary>Only a member with the system role Owner passes.</summary>
  Task<ProjectAccessEntry> EnsureOwnerAsync(int projectId, CancellationToken cancellationToken);
}

public sealed class ProjectAccess(AppDbContext dbContext, IAccessCache cache) : IProjectAccess
{
  private LoggedUser? user;

  public void Bind(LoggedUser? user) => this.user = user;

  private int UserId =>
    user?.AppUserId
    ?? throw new AuthenticationFailedException(AuthenticationFailedException.FailureReason.TokenNotProvided, null);

  public async Task EnsureActiveUserAsync(CancellationToken cancellationToken)
  {
    var userId = UserId;
    if (!cache.TryGetUser(userId, out var status, out var version))
    {
      status = await dbContext
        .AppUsers //
        .AsNoTracking()
        .Where(u => u.Id == userId)
        .Select(u => new UserStatus(true, u.IsActive))
        .FirstOrDefaultAsync(cancellationToken) ?? new UserStatus(false, false);
      cache.SetUser(userId, version, status);
    }

    if (!status!.CanUseApi)
      throw new AuthenticationFailedException(AuthenticationFailedException.FailureReason.TokenNotFound, null);
  }

  public async Task<ProjectAccessEntry> GetAsync(int projectId, CancellationToken cancellationToken)
  {
    var userId = UserId;
    if (!cache.TryGetAccess(userId, projectId, out var entry, out var version))
    {
      var role = await dbContext
        .Memberships //
        .AsNoTracking()
        .Where(m => m.UserId == userId && m.ProjectId == projectId)
        .Select(m => m.Role)
        .FirstOrDefaultAsync(cancellationToken);
      entry = role == null ? ProjectAccessEntry.NotMember : ProjectAccessEntry.From(role);
      cache.SetAccess(userId, projectId, version, entry);
    }

    if (!entry!.IsMember)
      throw new EntityNotFoundException(typeof(Project), projectId);
    return entry;
  }

  public async Task<ProjectAccessEntry> EnsureCanAsync(
    int projectId,
    ProjectPermission permission,
    CancellationToken cancellationToken
  )
  {
    var entry = await GetAsync(projectId, cancellationToken);
    if (!entry.Has(permission))
      throw new AuthorizationFailedException();
    return entry;
  }

  public async Task<ProjectAccessEntry> EnsureOwnerAsync(int projectId, CancellationToken cancellationToken)
  {
    var entry = await GetAsync(projectId, cancellationToken);
    if (!entry.IsOwner)
      throw new AuthorizationFailedException();
    return entry;
  }
}

/// <summary>Skips every check; for trusted internal callers only (development seed).</summary>
public sealed class AllowAllProjectAccess : IProjectAccess
{
  public static readonly AllowAllProjectAccess Instance = new();

  public void Bind(LoggedUser? user) { }

  public Task EnsureActiveUserAsync(CancellationToken cancellationToken) => Task.CompletedTask;

  public Task<ProjectAccessEntry> GetAsync(int projectId, CancellationToken cancellationToken) =>
    Task.FromResult(ProjectAccessEntry.Everything);

  public Task<ProjectAccessEntry> EnsureCanAsync(int projectId, ProjectPermission permission, CancellationToken cancellationToken) =>
    GetAsync(projectId, cancellationToken);

  public Task<ProjectAccessEntry> EnsureOwnerAsync(int projectId, CancellationToken cancellationToken) =>
    GetAsync(projectId, cancellationToken);
}

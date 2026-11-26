using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Model.Db;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Services;

public static class MembershipRules
{
  /// <summary>
  /// Call after the change was saved but before the transaction is committed: a project must always keep
  /// at least one owner and at least one member who can manage members (otherwise nobody could fix it).
  /// </summary>
  public static async Task EnsureProjectStaysManageableAsync(
    AppDbContext db,
    int projectId,
    CancellationToken cancellationToken
  )
  {
    var members = db.Memberships.Where(m => m.ProjectId == projectId);
    if (!await members.AnyAsync(m => m.Role.SystemKind == RoleSystemKind.Owner, cancellationToken))
      throw new BadRequestException("The project must keep at least one owner");
    if (!await members.AnyAsync(m => m.Role.CanManageMembers, cancellationToken))
      throw new BadRequestException("The project must keep at least one member who can manage members");
  }
}

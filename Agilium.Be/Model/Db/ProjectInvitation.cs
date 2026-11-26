namespace Eng.Agilium.Be.Model.Db;

/// <summary>
/// An invitation key that lets its holder join a project with the given role. Only the SHA-256 hash of the key
/// is stored; the key itself is shown once, when the invitation is created.
/// </summary>
public class ProjectInvitation
{
  public const int DefaultValidityDays = 7;
  public const int MaxValidityDays = 90;
  public const int DefaultMaxUses = 10;
  public const int MaxMaxUses = 1000;

  public int Id { get; set; }
  public int ProjectId { get; set; }
  public Project Project { get; set; } = null!;
  public int RoleId { get; set; }
  public Role Role { get; set; } = null!;
  public string KeyHash { get; set; } = string.Empty;
  public string KeyPrefix { get; set; } = string.Empty;
  public DateTime ExpiresAt { get; set; }
  public int MaxUses { get; set; }
  public int UsedCount { get; set; }
  public int CreatedById { get; set; }
  public AppUser CreatedBy { get; set; } = null!;
  public DateTime CreatedAt { get; set; }
  public DateTime? RevokedAt { get; set; }

  public InvitationStatus StatusAt(DateTime now) =>
    RevokedAt != null ? InvitationStatus.Revoked
    : ExpiresAt <= now ? InvitationStatus.Expired
    : UsedCount >= MaxUses ? InvitationStatus.Exhausted
    : InvitationStatus.Active;
}

public enum InvitationStatus
{
  Active = 1,
  Expired = 2,
  Exhausted = 3,
  Revoked = 4,
}

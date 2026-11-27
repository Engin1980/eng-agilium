namespace Eng.Agilium.Be.Model.Db;

public class AppUser
{
  public int Id { get; set; }
  public string Email { get; set; } = string.Empty;
  public string Name { get; set; } = string.Empty;
  public string Surname { get; set; } = string.Empty;
  public string PasswordHash { get; set; } = string.Empty;
  public bool IsActive { get; set; }
  /// <summary>Null until the owner of the mailbox confirms the e-mail address; such an account cannot log in.</summary>
  public DateTime? EmailVerifiedAt { get; set; }
  public DateTime? IsLockedUntil { get; set; }
  public int FailedLoginAttemptsCount { get; set; }
  public DateTime? LastSuccessfulLogin { get; set; }
  public ICollection<Token> Tokens { get; set; } = [];
  public ICollection<Membership> Memberships { get; set; } = [];
}

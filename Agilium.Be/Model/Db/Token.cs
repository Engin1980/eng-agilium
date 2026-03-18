namespace Eng.Agilium.Be.Model.Db;

public class Token
{
  public int Id { get; set; }
  public string Value { get; set; } = string.Empty;
  public TokenType Type { get; set; }
  public DateTime Expiration { get; set; }
  public int AppUserId { get; set; }
  public AppUser AppUser { get; set; } = null!;
}

public enum TokenType
{
  PasswordReset = 1,
  Refresh = 2,
}

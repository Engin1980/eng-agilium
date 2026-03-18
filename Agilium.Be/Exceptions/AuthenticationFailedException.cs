using Eng.Agilium.Be.Model.Db;

namespace Eng.Agilium.Be.Exceptions;

public class AuthenticationFailedException : BadRequestException
{
  public enum FailureReason
  {
    EmailNotFound,
    CredentialsInvalid,
    TokenNotFound,
    TokenExpired,
    TokenNotProvided,
  }

  public AuthenticationFailedException(FailureReason reason, AppUser? entity)
    : base("Authentication failed.")
  {
    Reason = reason;
    Entity = entity;
  }

  public FailureReason Reason { get; }
  public AppUser? Entity { get; }
}

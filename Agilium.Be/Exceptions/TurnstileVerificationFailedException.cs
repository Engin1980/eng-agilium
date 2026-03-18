namespace Eng.Agilium.Be.Exceptions;

public class TurnstileVerificationFailedException(string[] errorCodes)
  : BadRequestException("Turnstille verification failed: " + string.Join("; ", errorCodes))
{
  public string[] ErrorCodes { get; } = errorCodes;
}
using Microsoft.Extensions.Options;

namespace Eng.Agilium.Be.Services;

public class AppSettingsService(IOptions<AppSettings> options)
{
  public AppSettings AppSettings { get; private set; } = options.Value;
}

public record AppSettings
{
  public required SecuritySettings Security { get; init; }
  public required EmailSettings Email { get; init; }
  public required string FrontendBaseUrl { get; init; }
  public required bool UseSwagger { get; init; }
}

public record EmailSettings
{
  public required EmailSmtpSettings Smtp { get; init; }
  public string? DebugRecipient { get; init; }
}

public record EmailSmtpSettings
{
  public required string Host { get; init; }
  public required int Port { get; init; }
  public required string Username { get; init; }
  public required string Password { get; init; }
}

public record SecuritySettings
{
  public required JwtSettings Jwt { get; init; }
  public required TokensSettings Tokens { get; init; }
  public required PasswordSettings Password { get; init; }
  public required TurnstileSettings Turnstile { get; init; }
  public required RequestLimiterSettings RequestLimiter { get; init; }
  public bool EnableTestingEndpoints { get; init; }
}

public record RequestLimiterSettings
{
  public required int WindowLength { get; init; }
  public required int SegmentsPerWindow { get; init; }
  public required int PermitLimit { get; init; }
  public required int QueueLimit { get; init; }
}

public class TurnstileSettings
{
  public required bool Enabled { get; init; }
  public required string SecretKey { get; init; }
}

public record PasswordSettings
{
  public required int MaxFailedLoginAttempts { get; init; }
  public required int LockoutDurationMinutes { get; init; }
}

public record JwtSettings
{
  public required string Key { get; init; }
  public required string Issuer { get; init; }
}

public record TokensSettings
{
  public required int AccessTokenExpirationMinutes { get; init; }
  public required int RefreshTokenExpirationMinutes { get; init; }
  public required int RefreshTokenLength { get; init; }
  public required int PasswordResetTokenExpirationMinutes { get; init; }
  public required int PasswordResetTokenLength { get; init; }
}

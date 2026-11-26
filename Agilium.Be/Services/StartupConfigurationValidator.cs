namespace Eng.Agilium.Be.Services;

/// <summary>
/// Refuses to start the application with a configuration that is only acceptable for local development
/// (default JWT key, disabled Turnstile, placeholders from the committed appsettings.json...).
/// </summary>
public static class StartupConfigurationValidator
{
  public const string OverridePlaceholder = "(override)";
  public const int MinJwtKeyLength = 32;

  /// <summary>The key committed in appsettings.json - anybody can forge tokens signed with it.</summary>
  private const string DefaultJwtKeyMarker = "ThisIsASecretKeyForJWT";

  private const string DefaultTurnstileKeyMarker = "ThisIsASecretKeyForTurnstile";

  /// <summary>Environments that do not have to be production-grade (local development, automated tests).</summary>
  public static bool IsRelaxedEnvironment(IHostEnvironment environment) =>
    environment.IsDevelopment() || environment.IsEnvironment("Testing");

  /// <summary>Rules that are wrong in every environment (they would break the application, not just weaken it).</summary>
  public static List<string> ValidateAlways(AppSettings settings)
  {
    var errors = new List<string>();
    var tokens = settings.Security.Tokens;

    if (tokens.AccessTokenExpirationMinutes < 1)
      errors.Add("Security:Tokens:AccessTokenExpirationMinutes must be positive.");
    if (tokens.RefreshTokenExpirationMinutes < tokens.AccessTokenExpirationMinutes)
      errors.Add(
        "Security:Tokens:RefreshTokenExpirationMinutes must not be shorter than AccessTokenExpirationMinutes "
          + "(the session could not be renewed). Both values are in minutes."
      );

    return errors;
  }

  /// <summary>Rules for everything that is not development / test: no insecure defaults may remain.</summary>
  public static List<string> ValidateForProduction(AppSettings settings, string? connectionString)
  {
    var errors = new List<string>();
    var security = settings.Security;

    if (IsMissing(connectionString))
      errors.Add("ConnectionStrings:DefaultConnection is not configured.");

    var jwtKey = security.Jwt.Key ?? "";
    if (jwtKey.Length < MinJwtKeyLength)
      errors.Add($"Security:Jwt:Key must be at least {MinJwtKeyLength} characters long.");
    if (jwtKey.Contains(DefaultJwtKeyMarker, StringComparison.Ordinal))
      errors.Add("Security:Jwt:Key is still the default key from appsettings.json - tokens could be forged.");

    if (!security.Turnstile.Enabled)
      errors.Add("Security:Turnstile:Enabled must be true.");
    if (
      IsMissing(security.Turnstile.SecretKey)
      || security.Turnstile.SecretKey.Contains(DefaultTurnstileKeyMarker, StringComparison.Ordinal)
    )
      errors.Add("Security:Turnstile:SecretKey is missing or still the placeholder from appsettings.json.");

    if (security.EnableTestingEndpoints)
      errors.Add("Security:EnableTestingEndpoints must be false.");

    var smtp = settings.Email.Smtp;
    if (IsMissing(smtp.Host))
      errors.Add("Email:Smtp:Host is not configured.");
    if (smtp.Port is < 1 or > 65535)
      errors.Add("Email:Smtp:Port is not valid.");
    if (smtp.Username == OverridePlaceholder || smtp.Password == OverridePlaceholder)
      errors.Add("Email:Smtp:Username / Password are not configured (they are '" + OverridePlaceholder + "').");

    if (IsMissing(settings.FrontendBaseUrl))
      errors.Add("FrontendBaseUrl is not configured.");

    return errors;
  }

  /// <summary>Throws <see cref="InvalidOperationException"/> listing every problem found.</summary>
  public static void EnsureValid(AppSettings settings, string? connectionString, IHostEnvironment environment)
  {
    var errors = ValidateAlways(settings);
    if (!IsRelaxedEnvironment(environment))
      errors.AddRange(ValidateForProduction(settings, connectionString));

    if (errors.Count > 0)
      throw new InvalidOperationException(
        "Invalid configuration:" + Environment.NewLine + string.Join(Environment.NewLine, errors.Select(e => " - " + e))
      );
  }

  private static bool IsMissing(string? value) =>
    string.IsNullOrWhiteSpace(value) || value.Trim() == OverridePlaceholder;
}

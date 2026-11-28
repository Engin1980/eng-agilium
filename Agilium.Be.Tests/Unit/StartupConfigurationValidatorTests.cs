using Eng.Agilium.Be.Services;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Shouldly;

namespace Eng.Agilium.Be.Tests.Unit;

public class StartupConfigurationValidatorTests
{
  private sealed class FakeEnvironment(string name) : IHostEnvironment
  {
    public string EnvironmentName { get; set; } = name;
    public string ApplicationName { get; set; } = "test";
    public string ContentRootPath { get; set; } = ".";
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
  }

  private static AppSettings ValidProduction(
    string jwtKey = "a-really-long-random-production-key-0123456789",
    bool turnstile = true,
    string turnstileKey = "real-turnstile-secret",
    string turnstileSiteKey = "real-turnstile-site-key",
    string smtpHost = "smtp.example.com",
    string smtpUser = "user",
    int accessMinutes = 15,
    int refreshMinutes = 10080
  ) =>
    new()
    {
      FrontendBaseUrl = "https://app.example.com",
      UseSwagger = false,
      Email = new EmailSettings
      {
        Smtp = new EmailSmtpSettings
        {
          Host = smtpHost,
          Port = 587,
          Username = smtpUser,
          Password = "secret",
        },
      },
      Security = new SecuritySettings
      {
        Jwt = new JwtSettings { Key = jwtKey, Issuer = "issuer" },
        Turnstile = new TurnstileSettings { Enabled = turnstile, SecretKey = turnstileKey, SiteKey = turnstileSiteKey },
        Password = new PasswordSettings { MaxFailedLoginAttempts = 5, LockoutDurationMinutes = 15 },
        RequestLimiter = new RequestLimiterSettings
        {
          WindowLength = 10,
          SegmentsPerWindow = 5,
          PermitLimit = 100,
          QueueLimit = 20,
        },
        Tokens = new TokensSettings
        {
          AccessTokenExpirationMinutes = accessMinutes,
          RefreshTokenExpirationMinutes = refreshMinutes,
          RefreshTokenLength = 32,
          PasswordResetTokenExpirationMinutes = 15,
          PasswordResetTokenLength = 32,
        },
      },
    };

  private const string GoodConnection = "Server=db;Database=x;User Id=sa;Password=p;";

  [Fact]
  public void ValidProductionConfiguration_HasNoErrors() =>
    StartupConfigurationValidator.ValidateForProduction(ValidProduction(), GoodConnection).ShouldBeEmpty();

  [Theory]
  [InlineData("short")]
  [InlineData("ThisIsASecretKeyForJWT-ThisIsASecretKeyForJWT-ThisIsASecretKeyForJWT")]
  public void WeakOrDefaultJwtKey_IsRejected(string key) =>
    StartupConfigurationValidator
      .ValidateForProduction(ValidProduction(jwtKey: key), GoodConnection)
      .ShouldContain(e => e.Contains("Jwt:Key"));

  [Fact]
  public void DisabledTurnstile_IsAccepted() =>
    StartupConfigurationValidator
      .ValidateForProduction(ValidProduction(turnstile: false, turnstileKey: ""), GoodConnection)
      .ShouldBeEmpty();

  [Theory]
  [InlineData("", "site")]
  [InlineData("(override)", "site")]
  [InlineData("secret", "")]
  public void EnabledTurnstileWithoutKeys_IsRejectedEverywhere(string secret, string site) =>
    StartupConfigurationValidator
      .ValidateAlways(ValidProduction(turnstileKey: secret, turnstileSiteKey: site))
      .ShouldContain(e => e.Contains("Turnstile:Enabled is true"));

  [Fact]
  public void EnsureValid_InDevelopment_RejectsEnabledTurnstileWithoutKeys() =>
    Should.Throw<InvalidOperationException>(() =>
      StartupConfigurationValidator.EnsureValid(
        ValidProduction(turnstileKey: "", turnstileSiteKey: ""),
        null,
        new FakeEnvironment("Development")
      )
    );

  [Fact]
  public void PlaceholderTurnstileSecret_IsRejected() =>
    StartupConfigurationValidator
      .ValidateForProduction(ValidProduction(turnstileKey: "ThisIsASecretKeyForTurnstile-xyz"), GoodConnection)
      .ShouldContain(e => e.Contains("Turnstile:SecretKey"));

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("(override)")]
  public void MissingConnectionString_IsRejected(string? connection) =>
    StartupConfigurationValidator
      .ValidateForProduction(ValidProduction(), connection)
      .ShouldContain(e => e.Contains("DefaultConnection"));

  [Fact]
  public void UnsetSmtp_IsRejected()
  {
    var errors = StartupConfigurationValidator.ValidateForProduction(
      ValidProduction(smtpHost: "(override)", smtpUser: "(override)"),
      GoodConnection
    );

    errors.ShouldContain(e => e.Contains("Smtp:Host"));
    errors.ShouldContain(e => e.Contains("Smtp:Username"));
  }

  [Fact]
  public void RefreshTokenShorterThanAccessToken_IsRejectedEverywhere()
  {
    // the typo that used to be in appsettings.json: 15 minutes access, 7 (minutes) refresh
    var errors = StartupConfigurationValidator.ValidateAlways(ValidProduction(accessMinutes: 15, refreshMinutes: 7));

    errors.ShouldContain(e => e.Contains("RefreshTokenExpirationMinutes"));
    StartupConfigurationValidator.ValidateAlways(ValidProduction()).ShouldBeEmpty();
  }

  [Fact]
  public void EnsureValid_InDevelopment_AcceptsDevelopmentDefaults() =>
    Should.NotThrow(() =>
      StartupConfigurationValidator.EnsureValid(
        ValidProduction(jwtKey: "short", turnstile: false, smtpHost: "(override)"),
        null,
        new FakeEnvironment("Development")
      )
    );

  [Fact]
  public void EnsureValid_InProduction_ListsAllProblems()
  {
    var ex = Should.Throw<InvalidOperationException>(() =>
      StartupConfigurationValidator.EnsureValid(
        ValidProduction(jwtKey: "short", turnstile: false),
        null,
        new FakeEnvironment("Production")
      )
    );

    ex.Message.ShouldContain("Jwt:Key");
    ex.Message.ShouldContain("DefaultConnection");
  }
}

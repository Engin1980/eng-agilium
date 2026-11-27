using Eng.Agilium.Be.Model.Db;

namespace Eng.Agilium.Be.Services;

/// <summary>
/// Account e-mails (verification, password reset, "account exists"). A failing SMTP server is logged, not thrown:
/// the anonymous endpoints must answer identically whether or not an account exists, and the user can ask again.
/// </summary>
public class AccountMailer(
  IEmailSender sender,
  TokenService tokenService,
  AppSettingsService appSettingsService,
  ILogger<AccountMailer> logger
)
{
  private readonly AppSettings settings = appSettingsService.AppSettings;

  public async Task SendVerificationAsync(AppUser user, CancellationToken cancellationToken)
  {
    var token = await tokenService.CreateEmailVerificationAsync(user);
    var link = EmailTemplates.VerifyLink(settings.FrontendBaseUrl, token);
    await SendSafeAsync(EmailTemplates.Verification(user.Email, user.Name, link), cancellationToken);
  }

  public async Task SendPasswordResetAsync(AppUser user, CancellationToken cancellationToken)
  {
    var token = await tokenService.CreatePasswordResetAsync(user);
    var link = EmailTemplates.ResetLink(settings.FrontendBaseUrl, token);
    var minutes = settings.Security.Tokens.PasswordResetTokenExpirationMinutes;
    await SendSafeAsync(EmailTemplates.PasswordReset(user.Email, user.Name, link, minutes), cancellationToken);
  }

  public Task SendAccountExistsAsync(AppUser user, CancellationToken cancellationToken) =>
    SendSafeAsync(
      EmailTemplates.AccountExists(user.Email, user.Name, $"{settings.FrontendBaseUrl.TrimEnd('/')}/login"),
      cancellationToken
    );

  private async Task SendSafeAsync(EmailMessage message, CancellationToken cancellationToken)
  {
    try
    {
      await sender.SendAsync(message, cancellationToken);
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
      logger.LogError(ex, "Sending the e-mail '{Subject}' failed", message.Subject);
    }
  }
}

using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Eng.Agilium.Be.Services;

public record EmailMessage(string To, string Subject, string TextBody, string HtmlBody);

/// <summary>Sends e-mails. Tests replace it with a fake that only records the messages.</summary>
public interface IEmailSender
{
  Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

public sealed class SmtpEmailSender(AppSettingsService appSettingsService, ILogger<SmtpEmailSender> logger) : IEmailSender
{
  private readonly EmailSettings settings = appSettingsService.AppSettings.Email;

  public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
  {
    // with DebugRecipient every e-mail goes to that address (testing against a real SMTP server)
    var recipient = string.IsNullOrWhiteSpace(settings.DebugRecipient) ? message.To : settings.DebugRecipient;

    var mime = new MimeMessage();
    mime.From.Add(MailboxAddress.Parse(settings.Smtp.Username.Contains('@') ? settings.Smtp.Username : "noreply@agilium.local"));
    mime.To.Add(MailboxAddress.Parse(recipient));
    mime.Subject = message.Subject;
    mime.Body = new BodyBuilder { TextBody = message.TextBody, HtmlBody = message.HtmlBody }.ToMessageBody();

    using var client = new SmtpClient();
    await client.ConnectAsync(settings.Smtp.Host, settings.Smtp.Port, SecureSocketOptions.Auto, cancellationToken);
    if (!string.IsNullOrEmpty(settings.Smtp.Username))
      await client.AuthenticateAsync(settings.Smtp.Username, settings.Smtp.Password, cancellationToken);
    await client.SendAsync(mime, cancellationToken);
    await client.DisconnectAsync(true, cancellationToken);
    logger.LogInformation("E-mail '{Subject}' sent", message.Subject);
  }
}

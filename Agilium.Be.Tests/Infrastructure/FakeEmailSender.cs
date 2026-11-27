using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Eng.Agilium.Be.Services;

namespace Eng.Agilium.Be.Tests.Infrastructure;

/// <summary>Records the e-mails instead of sending them.</summary>
public sealed class FakeEmailSender : IEmailSender
{
  private readonly ConcurrentQueue<EmailMessage> sent = new();

  public IReadOnlyList<EmailMessage> Sent => sent.ToArray();

  public bool Fail { get; set; }

  public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
  {
    if (Fail)
      throw new InvalidOperationException("SMTP is down");
    sent.Enqueue(message);
    return Task.CompletedTask;
  }

  public IReadOnlyList<EmailMessage> To(string email) =>
    Sent.Where(m => string.Equals(m.To, email, StringComparison.OrdinalIgnoreCase)).ToList();

  public void Clear() => sent.Clear();

  /// <summary>The token from the (last) link in a message body, e.g. <c>/verify-email?token=XYZ</c>.</summary>
  public static string TokenOf(EmailMessage message) =>
    Uri.UnescapeDataString(Regex.Match(message.TextBody, @"token=([^\s&]+)").Groups[1].Value);
}

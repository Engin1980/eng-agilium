using System.Net;

namespace Eng.Agilium.Be.Services;

/// <summary>Czech e-mail texts (plain text + HTML) with links into the frontend.</summary>
public static class EmailTemplates
{
  public static string VerifyLink(string frontendBaseUrl, string token) =>
    $"{frontendBaseUrl.TrimEnd('/')}/verify-email?token={Uri.EscapeDataString(token)}";

  public static string ResetLink(string frontendBaseUrl, string token) =>
    $"{frontendBaseUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(token)}";

  public static EmailMessage Verification(string to, string name, string link) =>
    Build(
      to,
      "Agilium: potvrďte svůj e-mail",
      name,
      "děkujeme za registraci. Svůj e-mail potvrdíte kliknutím na odkaz (platí 24 hodin):",
      link,
      "Pokud jste se neregistrovali, tento e-mail ignorujte."
    );

  public static EmailMessage AccountExists(string to, string name, string loginLink) =>
    Build(
      to,
      "Agilium: účet už existuje",
      name,
      "někdo se právě pokusil zaregistrovat tento e-mail, ale účet už existuje. Můžete se přihlásit (a případně použít „Zapomenuté heslo“):",
      loginLink,
      "Pokud jste to nebyli vy, nemusíte nic dělat."
    );

  public static EmailMessage PasswordReset(string to, string name, string link, int minutes) =>
    Build(
      to,
      "Agilium: obnovení hesla",
      name,
      $"pro obnovení hesla klikněte na odkaz (platí {minutes} minut):",
      link,
      "Pokud jste o obnovení hesla nežádali, tento e-mail ignorujte."
    );

  private static EmailMessage Build(string to, string subject, string name, string lead, string link, string footer)
  {
    var greeting = string.IsNullOrWhiteSpace(name) ? "Dobrý den," : $"Dobrý den, {name},";
    var text = $"{greeting}\n\n{lead}\n{link}\n\n{footer}\n";
    var html =
      $"<p>{WebUtility.HtmlEncode(greeting)}</p><p>{WebUtility.HtmlEncode(lead)}</p>"
      + $"<p><a href=\"{WebUtility.HtmlEncode(link)}\">{WebUtility.HtmlEncode(link)}</a></p><p>{WebUtility.HtmlEncode(footer)}</p>";
    return new EmailMessage(to, subject, text, html);
  }
}

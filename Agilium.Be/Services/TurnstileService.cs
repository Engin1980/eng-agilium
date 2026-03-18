using Newtonsoft.Json;

namespace Eng.Agilium.Be.Services;

public record TurnstileResponse(
  [property: JsonProperty("success")] bool Success,
  [property: JsonProperty("error-codes")] string[] ErrorCodes,
  [property: JsonProperty("challenge_ts")] DateTime? ChallengeTs = null,
  [property: JsonProperty("hostname")] string? Hostname = null,
  [property: JsonProperty("action")] string? Action = null,
  [property: JsonProperty("cdata")] string? CData = null,
  [property: JsonProperty("metadata")] ApiMetadata? Metadata = null
);

public record ApiMetadata([property: JsonProperty("ephemeral_id")] string EphemeralId);

public class TurnstileService(AppSettingsService appSettings)
{
  private const string TURNSTILE_URL = "https://challenges.cloudflare.com/turnstile/v0/siteverify";
  private readonly TurnstileSettings turnstileSettings = appSettings.AppSettings.Security.Turnstile;

  public async Task<TurnstileResponse> ValidateTurnstileTokenAsync(HttpContext httpContext, string token)
  {
    var remoteip =
      httpContext.Request.Headers["CF-Connecting-IP"].FirstOrDefault()
      ?? httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
      ?? httpContext.Connection.RemoteIpAddress?.ToString();
    return await ValidateTurnstileTokenAsync(remoteip, token);
  }

  public async Task<TurnstileResponse> ValidateTurnstileTokenAsync(string? remoteIp, string token)
  {
    if (!turnstileSettings.Enabled)
    {
      return new TurnstileResponse(true, [], null, null, null, null, null); // If Turnstile is disabled, always return success
    }

    TurnstileResponse ret;
    Dictionary<string, string> parameters = [];
    parameters["secret"] = turnstileSettings.SecretKey;
    parameters["response"] = token;
    if (!string.IsNullOrEmpty(remoteIp))
      parameters["remoteip"] = remoteIp;
    //TODO add 'idempotency_key' challenge to improve security

    var content = new FormUrlEncodedContent(parameters);

    using var httpClient = new HttpClient();

    try
    {
      var response = await httpClient.PostAsync(TURNSTILE_URL, content);
      var stringContent = await response.Content.ReadAsStringAsync();

      ret =
        JsonConvert.DeserializeObject<TurnstileResponse>(stringContent)
        ?? new TurnstileResponse(false, ["null-response"], null, null, null, null, null);
    }
    catch (Exception ex)
    {
      ret = new TurnstileResponse(false, ["internal-error : " + ex.Message], null, null, null, null, null);
    }
    return ret;
  }
}

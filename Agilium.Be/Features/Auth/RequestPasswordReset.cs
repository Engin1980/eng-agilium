using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Exceptions.Validation;
using Eng.Agilium.Be.Model.Db;
using Eng.Agilium.Be.Services;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Auth.RequestPasswordReset;

public record Command([property: XEmailValidation] string Email, string? TurnstileToken);

public class Handler(
  AppDbContext dbContext,
  AccountMailer mailer,
  IActionLimiter limiter,
  TurnstileService turnstileService,
  AppSettingsService appSettingsService,
  IHttpContextAccessor httpContextAccessor
) : GenericHandler<Command, EmptyParameters, EmptyResult>
{
  public static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(60);
  public const int MaxPerIpPerMinute = 10;

  public override async Task<EmptyResult> HandleAsync(
    Command command,
    EmptyParameters parameters,
    CancellationToken cancellationToken
  )
  {
    var httpContext = httpContextAccessor.HttpContext;
    if (appSettingsService.AppSettings.Security.Turnstile.Enabled && httpContext != null)
    {
      var turnstile = await turnstileService.ValidateTurnstileTokenAsync(httpContext, command.TurnstileToken ?? "");
      if (!turnstile.Success)
        throw new TurnstileVerificationFailedException(turnstile.ErrorCodes);
    }

    var ip = httpContext?.Connection.RemoteIpAddress?.ToString();
    if (ip != null && !limiter.TryAcquire($"reset-ip:{ip}", MaxPerIpPerMinute, TimeSpan.FromMinutes(1)))
      throw new TooManyRequestsException("Too many requests, try again later");

    var email = command.Email.Trim().ToLowerInvariant();
    if (!limiter.TryAcquire($"reset:{email}", 1, Cooldown))
      return new EmptyResult();

    var user = await dbContext.AppUsers.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    if (user is { IsActive: true })
      await mailer.SendPasswordResetAsync(user, cancellationToken);
    return new EmptyResult();
  }
}

[EndpointSummary("Sends a password reset e-mail (always 204, never reveals whether the account exists)")]
public class Endpoint : GenericOkEndpoint<Command, EmptyParameters, Handler, EmptyResult>
{
  public override HttpMethod Method => HttpMethod.Post;
  public override BaseRoute BaseRoute => BaseRoute.Auth;
  public override string EndpointRoute => "password-reset/request";
  public override EndpointPolicy Policy => EndpointPolicy.Anonymous;
}

using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Exceptions.Validation;
using Eng.Agilium.Be.Model.Db;
using Eng.Agilium.Be.Services;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Auth.ResendVerification;

public record Command([property: XEmailValidation] string Email);

public class Handler(
  AppDbContext dbContext,
  AccountMailer mailer,
  IActionLimiter limiter,
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
    var ip = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    if (ip != null && !limiter.TryAcquire($"verify-ip:{ip}", MaxPerIpPerMinute, TimeSpan.FromMinutes(1)))
      throw new TooManyRequestsException("Too many requests, try again later");

    var email = command.Email.Trim().ToLowerInvariant();
    // at most one e-mail per account and minute; the answer is the same in every case
    if (!limiter.TryAcquire($"verify:{email}", 1, Cooldown))
      return new EmptyResult();

    var user = await dbContext.AppUsers.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    if (user is { IsActive: true, EmailVerifiedAt: null })
      await mailer.SendVerificationAsync(user, cancellationToken);
    return new EmptyResult();
  }
}

[EndpointSummary("Sends the verification e-mail again (always 204; at most once a minute per account)")]
public class Endpoint : GenericOkEndpoint<Command, EmptyParameters, Handler, EmptyResult>
{
  public override HttpMethod Method => HttpMethod.Post;
  public override BaseRoute BaseRoute => BaseRoute.Auth;
  public override string EndpointRoute => "verify-email/resend";
  public override EndpointPolicy Policy => EndpointPolicy.Anonymous;
}

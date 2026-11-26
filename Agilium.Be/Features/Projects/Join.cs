using System.Data;
using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Exceptions.Validation;
using Eng.Agilium.Be.Features.Invitations.Create;
using Eng.Agilium.Be.Model.Db;
using Eng.Agilium.Be.Services;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Projects.Join;

public record Command([property: XNonEmpty(XValidationErrorKey.INVALID_TITLE)] string Key);

public record Result(int ProjectId);

public class Handler(
  AppDbContext dbContext,
  IAccessCache accessCache,
  IJoinAttemptLimiter limiter,
  IHttpContextAccessor httpContextAccessor,
  TimeProvider clock
) : GenericHandler<Command, EmptyParameters, Result>
{
  /// <summary>One answer for every invalid case, so the response does not reveal why the key does not work.</summary>
  private const string InvalidKey = "Invalid or expired key";

  public override async Task<Result> HandleAsync(
    Command command,
    EmptyParameters parameters,
    CancellationToken cancellationToken
  )
  {
    var userId = LoggedUser?.AppUserId ?? throw new InvalidOperationException("Login is required.");
    var ip = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    limiter.EnsureAllowed(userId, ip);

    var hash = InvitationKeys.Hash(command.Key);
    var now = clock.GetUtcNow().UtcDateTime;
    int projectId;

    using (var tx = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken))
    {
      var invitation = await dbContext.ProjectInvitations.FirstOrDefaultAsync(i => i.KeyHash == hash, cancellationToken);
      if (invitation == null || invitation.StatusAt(now) != InvitationStatus.Active)
      {
        limiter.RegisterFailure(userId, ip);
        throw new BadRequestException(InvalidKey);
      }

      projectId = invitation.ProjectId;
      if (await dbContext.Memberships.AnyAsync(m => m.ProjectId == projectId && m.UserId == userId, cancellationToken))
        throw new EntityAlreadyExistsException(typeof(Membership), projectId.ToString());

      dbContext.Memberships.Add(new Membership { ProjectId = projectId, UserId = userId, RoleId = invitation.RoleId });
      invitation.UsedCount++;
      await dbContext.SaveChangesAsync(cancellationToken);
      await tx.CommitAsync(cancellationToken);
    }

    accessCache.InvalidateProject(projectId);
    return new Result(projectId);
  }
}

[EndpointSummary("Joins the logged-in user to a project using an invitation key")]
public class Endpoint : GenericOkEndpoint<Command, EmptyParameters, Handler, Result>
{
  public override HttpMethod Method => HttpMethod.Post;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "join";
  public override EndpointPolicy Policy => EndpointPolicy.LoggedIn;
}

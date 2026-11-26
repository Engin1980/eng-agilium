using Eng.Agilium.Be.Model.Db;
using Eng.Agilium.Be.Services;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Invitations.List;

public record InvitationResult(
  int Id,
  string KeyPrefix,
  int RoleId,
  string RoleTitle,
  DateTime ExpiresAt,
  int MaxUses,
  int UsedCount,
  DateTime CreatedAt,
  int Status
);

public record Result(IEnumerable<InvitationResult> Invitations);

public class Handler(AppDbContext dbContext, TimeProvider clock) : GenericHandler<EmptyCommand, IdParameters, Result>
{
  public override async Task<Result> HandleAsync(
    EmptyCommand command,
    IdParameters parameters,
    CancellationToken cancellationToken
  )
  {
    await Access.EnsureCanAsync(parameters.Id, ProjectPermission.ManageMembers, cancellationToken);

    var now = clock.GetUtcNow().UtcDateTime;
    var invitations = await dbContext
      .ProjectInvitations //
      .AsNoTracking()
      .Include(i => i.Role)
      .Where(i => i.ProjectId == parameters.Id)
      .OrderByDescending(i => i.CreatedAt)
      .ToListAsync(cancellationToken);

    // the key itself is never returned (only its hash is stored)
    return new Result(
      invitations.Select(i => new InvitationResult(
        i.Id,
        i.KeyPrefix,
        i.RoleId,
        i.Role.Title,
        i.ExpiresAt,
        i.MaxUses,
        i.UsedCount,
        i.CreatedAt,
        (int)i.StatusAt(now)
      ))
    );
  }
}

[EndpointSummary("Lists the invitations of a project (status: 1 active, 2 expired, 3 exhausted, 4 revoked)")]
public class Endpoint : GenericOkEndpoint<EmptyCommand, IdParameters, Handler, Result>
{
  public override HttpMethod Method => HttpMethod.Get;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "{id}/invitations";
  public override EndpointPolicy Policy => EndpointPolicy.Project(ProjectPermission.ManageMembers);
}

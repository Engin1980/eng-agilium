using Eng.Agilium.Be.Model.Db;
using Eng.Agilium.Be.Services;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Projects.ListMembers;

public record MemberResult(
  int MembershipId,
  int UserId,
  string Name,
  string Surname,
  string Email,
  int RoleId,
  string RoleTitle
);

public record Result(IEnumerable<MemberResult> Members);

public class Handler(AppDbContext dbContext) : GenericHandler<EmptyCommand, IdParameters, Result>
{
  public override async Task<Result> HandleAsync(
    EmptyCommand command,
    IdParameters parameters,
    CancellationToken cancellationToken
  )
  {
    await Access.EnsureCanAsync(parameters.Id, ProjectPermission.ViewMembers, cancellationToken);

    var members = await dbContext
      .Memberships //
      .AsNoTracking()
      .Where(m => m.ProjectId == parameters.Id)
      .OrderBy(m => m.User.Surname)
      .ThenBy(m => m.User.Name)
      .Select(m => new MemberResult(m.Id, m.UserId, m.User.Name, m.User.Surname, m.User.Email, m.RoleId, m.Role.Title))
      .ToListAsync(cancellationToken);

    return new Result(members);
  }
}

[EndpointSummary("Lists the members of a project with their roles")]
public class Endpoint : GenericOkEndpoint<EmptyCommand, IdParameters, Handler, Result>
{
  public override HttpMethod Method => HttpMethod.Get;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "{id}/members";
  public override EndpointPolicy Policy => EndpointPolicy.Project(ProjectPermission.ViewMembers);
}

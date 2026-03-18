using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Model.Db;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Projects.AssignMember;

public record Command(int AppUserId, int RoleId);

public class Handler(AppDbContext dbContext) : GenericHandler<Command, IdParameters, IdResult>
{
  private readonly AppDbContext dbContext = dbContext;

  public override async Task<IdResult> HandleAsync(
    Command command,
    IdParameters parameters,
    CancellationToken cancellationToken
  )
  {
    var project =
      await dbContext.Projects.FirstOrDefaultAsync(p => p.Id == parameters.Id, cancellationToken)
      ?? throw new EntityNotFoundException(typeof(Project), parameters.Id);

    var user =
      await dbContext.AppUsers.FirstOrDefaultAsync(u => u.Id == command.AppUserId, cancellationToken)
      ?? throw new EntityNotFoundException(typeof(AppUser), command.AppUserId);

    var role =
      await dbContext.Roles.FirstOrDefaultAsync(r => r.Id == command.RoleId, cancellationToken)
      ?? throw new EntityNotFoundException(typeof(Role), command.RoleId);

    if (role.ProjectId != project.Id)
      throw new BadRequestException("Role does not belong to the project");

    Membership? membership = await dbContext.Memberships.FirstOrDefaultAsync(
      m => m.ProjectId == project.Id && m.UserId == user.Id,
      cancellationToken
    );

    if (membership == null)
    {
      membership = new Membership
      {
        Project = project,
        User = user,
        Role = role,
      };
      await dbContext.Memberships.AddAsync(membership, cancellationToken);
    }
    else
    {
      membership.Role = role;
      dbContext.Memberships.Update(membership);
    }

    await dbContext.SaveChangesAsync(cancellationToken);

    return new IdResult(membership.Id);
  }
}

[EndpointSummary(
  "Assigns an app user to a project with specified role. Returns always HTTP 200 + id of the created or updated membership."
)]
public class Endpoint : GenericOkEndpoint<Command, IdParameters, Handler, IdResult>
{
  public override HttpMethod Method => HttpMethod.Put;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "{id}/membership";
  public override string[] RequiredRoles => [];
}

using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Exceptions.Validation;
using Eng.Agilium.Be.Model.Db;
using Eng.Agilium.Be.Services;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Roles.Create;

public record Command(
  [property: XNonEmpty(XValidationErrorKey.INVALID_TITLE)] string Title,
  bool CanViewProject,
  bool CanManageProject,
  bool CanViewMembers,
  bool CanManageMembers,
  bool CanManageSprints,
  bool CanEditItems
);

public class Handler(AppDbContext dbContext) : GenericHandler<Command, IdParameters, IdResult>
{
  public override async Task<IdResult> HandleAsync(
    Command command,
    IdParameters parameters,
    CancellationToken cancellationToken
  )
  {
    await Access.EnsureCanAsync(parameters.Id, ProjectPermission.ManageMembers, cancellationToken);

    var title = command.Title.Trim();
    if (title.Length > 128)
      throw new BadRequestException("Role title is too long");
    if (await dbContext.Roles.AnyAsync(r => r.ProjectId == parameters.Id && r.Title == title, cancellationToken))
      throw new EntityAlreadyExistsException(typeof(Role), title);

    var role = new Role
    {
      ProjectId = parameters.Id,
      Title = title,
      SystemKind = RoleSystemKind.None,
      CanViewProject = command.CanViewProject,
      CanManageProject = command.CanManageProject,
      CanViewMembers = command.CanViewMembers,
      CanManageMembers = command.CanManageMembers,
      CanManageSprints = command.CanManageSprints,
      CanEditItems = command.CanEditItems,
    };
    dbContext.Roles.Add(role);
    await dbContext.SaveChangesAsync(cancellationToken);
    return new IdResult(role.Id);
  }
}

[EndpointSummary("Creates a custom role in a project")]
public class Endpoint : GenericCreatedEndpoint<Command, IdParameters, Handler, IdResult>
{
  public override HttpMethod Method => HttpMethod.Post;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "{id}/roles";
  public override EndpointPolicy Policy => EndpointPolicy.Project(ProjectPermission.ManageMembers);
}

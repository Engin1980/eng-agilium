using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Exceptions.Validation;
using Eng.Agilium.Be.Features.Templates;
using Eng.Agilium.Be.Model.Db;
using Microsoft.EntityFrameworkCore;

namespace Eng.Agilium.Be.Features.Projects.Create;

public record Command(
  [property: XNonEmpty(XValidationErrorKey.INVALID_TITLE)] string Title,
  [property: XNotNull(XValidationErrorKey.NULL_DESCRIPTION)] string Description
);

public class Handler(AppDbContext dbContext) : GenericHandler<Command, EmptyParameters, IdResult>
{
  public override async Task<IdResult> HandleAsync(
    Command command,
    EmptyParameters parameters,
    CancellationToken cancellationToken
  )
  {
    if (dbContext.Projects.Any(p => p.Title == command.Title))
      throw new EntityAlreadyExistsException(typeof(Project), command.Title);

    var project = new Project
    {
      Title = command.Title,
      Description = command.Description,
      State = ProjectState.Active,
    };

    using (var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken))
    {
      try
      {
        dbContext.Projects.Add(project);
        AddProjectRoles(project);
        AddOwnerMembership(project);
        await AddDefaultTemplatesAsync(project, cancellationToken);
        AddDefaultWorkflowStates(project);

        await dbContext.SaveChangesAsync(cancellationToken);

        await tx.CommitAsync(cancellationToken);
      }
      catch
      {
        await tx.RollbackAsync(cancellationToken);
        throw;
      }
    }

    return new IdResult(project.Id);
  }

  private void AddDefaultWorkflowStates(Project project)
  {
    project.WorkflowStates.Add(
      new WorkflowState()
      {
        Project = project,
        Title = "To Do",
        OrderIndex = 1,
        Type = WorkflowStateType.ToDo,
      }
    );
    project.WorkflowStates.Add(
      new WorkflowState()
      {
        Project = project,
        Title = "In Progress",
        OrderIndex = 2,
        Type = WorkflowStateType.Active,
      }
    );
    project.WorkflowStates.Add(
      new WorkflowState()
      {
        Project = project,
        Title = "Done",
        OrderIndex = 3,
        Type = WorkflowStateType.Done,
      }
    );
  }

  /// <summary>
  /// Gives the new project its own copy of each ItemType's global default template (see
  /// description.md: "při vytvoření projektu se udělá lokální kopie šablon"). Falls back to the
  /// built-in layout if a global template is somehow missing (it's seeded by AppInitializer).
  /// </summary>
  private async Task AddDefaultTemplatesAsync(Project project, CancellationToken cancellationToken)
  {
    var globalTemplates = await dbContext
      .Templates.AsNoTracking()
      .IncludeTree()
      .Where(t => t.ProjectId == null)
      .ToListAsync(cancellationToken);

    foreach (var type in Enum.GetValues<ItemType>())
      project.Templates.Add(TemplateTree.NewProjectTemplate(globalTemplates.FirstOrDefault(t => t.Type == type), type, project));
  }

  private void AddOwnerMembership(Project project)
  {
    var userId = LoggedUser?.AppUserId ?? throw new InvalidOperationException("Login is required.");

    Membership mi = new Membership
    {
      Project = project,
      UserId = userId,
      Role = project.Roles.First(r => r.SystemKind == RoleSystemKind.Owner),
    };
    project.Memberships.Add(mi);
  }

  private static void AddProjectRoles(Project project)
  {
    // see .docs/development/roles.md
    project.Roles.Add(
      new Role
      {
        Project = project,
        Title = "Owner",
        SystemKind = RoleSystemKind.Owner,
        CanViewProject = true,
        CanViewMembers = true,
        CanEditItems = true,
        CanManageSprints = true,
        CanManageProject = true,
        CanManageMembers = true,
      }
    );
    project.Roles.Add(
      new Role
      {
        Project = project,
        Title = "Manager",
        SystemKind = RoleSystemKind.Manager,
        CanViewProject = true,
        CanViewMembers = true,
        CanEditItems = true,
        CanManageSprints = true,
        CanManageProject = true,
        CanManageMembers = true,
      }
    );
    project.Roles.Add(
      new Role
      {
        Project = project,
        Title = "Member",
        SystemKind = RoleSystemKind.Member,
        CanViewProject = true,
        CanViewMembers = true,
        CanEditItems = true,
      }
    );
    project.Roles.Add(
      new Role
      {
        Project = project,
        Title = "Guest",
        SystemKind = RoleSystemKind.Guest,
        CanViewProject = true,
        CanViewMembers = true,
      }
    );
  }
}

[EndpointSummary("Creates a new project and returns its id")]
public class Endpoint : GenericCreatedEndpoint<Command, EmptyParameters, Handler, IdResult>
{
  public override HttpMethod Method => HttpMethod.Post;
  public override BaseRoute BaseRoute => BaseRoute.Projects;
  public override string EndpointRoute => "";
  public override EndpointPolicy Policy => EndpointPolicy.LoggedIn;
}

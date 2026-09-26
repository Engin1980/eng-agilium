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
  /// hardcoded field layout if a global template is somehow missing (it's seeded by AppInitializer).
  /// </summary>
  private async Task AddDefaultTemplatesAsync(Project project, CancellationToken cancellationToken)
  {
    var globalTemplates = await dbContext
      .Templates.AsNoTracking()
      .Include(t => t.TemplateItems)
      .Where(t => t.ProjectId == null)
      .ToListAsync(cancellationToken);

    foreach (var type in Enum.GetValues<ItemType>())
    {
      var global = globalTemplates.FirstOrDefault(t => t.Type == type);
      var sourceFields = global?.TemplateItems ?? DefaultTemplates.BuildFieldsFor(type);

      var template = new Template
      {
        Project = project,
        Type = type,
        ColumnCount = global?.ColumnCount ?? DefaultTemplates.DefaultColumnCount,
      };

      foreach (var field in sourceFields)
      {
        template.TemplateItems.Add(
          new TemplateItem
          {
            Key = field.Key,
            Title = field.Title,
            Type = field.Type,
            ValidatingRegex = field.ValidatingRegex,
            OrderIndex = field.OrderIndex,
            ColumnStart = field.ColumnStart,
            ColumnSpan = field.ColumnSpan,
            RowStart = field.RowStart,
            RowSpan = field.RowSpan,
          }
        );
      }

      project.Templates.Add(template);
    }
  }

  private void AddOwnerMembership(Project project)
  {
    System.Diagnostics.Debug.Assert(LoggedUser != null);

    Membership mi = new Membership
    {
      Project = project,
      UserId = LoggedUser.AppUserId,
      Role = project.Roles.First(r => r.Title == "Owner"),
    };
    project.Memberships.Add(mi);
  }

  private void AddProjectRoles(Project project)
  {
    project.Roles.Add(
      new Role
      {
        Project = project,
        Title = "Owner",
        CanManageMembers = true,
        CanManageProject = true,
        CanManageSprints = true,
        CanViewMembers = true,
        CanViewProject = true,
      }
    );
    project.Roles.Add(
      new Role
      {
        Project = project,
        Title = "Guest",
        CanManageMembers = false,
        CanManageProject = false,
        CanManageSprints = false,
        CanViewMembers = true,
        CanViewProject = true,
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
  public override string[] RequiredRoles => [];
}

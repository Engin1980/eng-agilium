using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Exceptions.Validation;
using Eng.Agilium.Be.Model.Db;

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
        AddDefaultTemplates(project);
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
        Type = WorkflowStateType.InProgress,
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

  private void AddDefaultTemplates(Project project)
  {
    var taskTypes = new[] { ItemType.Task, ItemType.Bug };

    foreach (var type in taskTypes)
    {
      Template template = new() { Project = project, Type = ItemType.Task };
      var left = new TemplateColumn() { Template = template, WidthWeight = 3 };
      var right = new TemplateColumn() { Template = template, WidthWeight = 1 };

      left.TemplateItems.Add(
        new TemplateItem()
        {
          OrderIndex = 1,
          Key = "description",
          Title = "Description",
          Type = TemplateItemType.NextlineTextArea,
        }
      );
      left.TemplateItems.Add(
        new TemplateItem()
        {
          OrderIndex = 2,
          Key = "comments",
          Title = "Comments",
          Type = TemplateItemType.Comments,
        }
      );

      right.TemplateItems.Add(
        new TemplateItem()
        {
          OrderIndex = 1,
          Key = "priority",
          Title = "Priority",
          Type = TemplateItemType.InlineInt,
        }
      );
      right.TemplateItems.Add(
        new TemplateItem()
        {
          OrderIndex = 1,
          Key = "complexity",
          Title = "Complexity",
          Type = TemplateItemType.InlineInt,
        }
      );
      right.TemplateItems.Add(
        new TemplateItem()
        {
          OrderIndex = 1,
          Key = "time-expected",
          Title = "Time Expected (h)",
          Type = TemplateItemType.InlineDouble,
        }
      );
      right.TemplateItems.Add(
        new TemplateItem()
        {
          OrderIndex = 1,
          Key = "time-spent",
          Title = "Time Spent (h)",
          Type = TemplateItemType.InlineDouble,
        }
      );

      project.Templates.Add(template);
    }

    var highTypes = new[] { ItemType.UserStory, ItemType.Feature, ItemType.Epic };

    foreach (var type in highTypes)
    {
      Template template = new() { Project = project, Type = ItemType.Task };
      var left = new TemplateColumn() { Template = template, WidthWeight = 3 };
      var right = new TemplateColumn() { Template = template, WidthWeight = 1 };

      left.TemplateItems.Add(
        new TemplateItem()
        {
          OrderIndex = 1,
          Key = "description",
          Title = "Description",
          Type = TemplateItemType.NextlineTextArea,
        }
      );
      left.TemplateItems.Add(
        new TemplateItem()
        {
          OrderIndex = 2,
          Key = "comments",
          Title = "Comments",
          Type = TemplateItemType.Comments,
        }
      );

      right.TemplateItems.Add(
        new TemplateItem()
        {
          OrderIndex = 1,
          Key = "priority",
          Title = "Priority",
          Type = TemplateItemType.InlineInt,
        }
      );
      right.TemplateItems.Add(
        new TemplateItem()
        {
          OrderIndex = 1,
          Key = "complexity",
          Title = "Complexity",
          Type = TemplateItemType.InlineInt,
        }
      );

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
      RoleId = project.Roles.First(r => r.Title == "Owner").Id,
    };
    project.Memberships.Add(mi);
  }

  private void AddProjectRoles(Project project)
  {
    List<Role> roles =
    [
      new Role
      {
        Project = project,
        Title = "Owner",
        CanManageMembers = true,
        CanManageProject = true,
        CanManageSprints = true,
        CanViewMembers = true,
        CanViewProject = true,
      },
      new Role
      {
        Project = project,
        Title = "Guest",
        CanManageMembers = false,
        CanManageProject = false,
        CanManageSprints = false,
        CanViewMembers = true,
        CanViewProject = true,
      },
    ];
    roles.ForEach(roles.Add);
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

using Eng.Agilium.Be.Features;
using Eng.Agilium.Be.Model.Db;
using Microsoft.EntityFrameworkCore;
using ItemsCreate = Eng.Agilium.Be.Features.Items.Create;
using ProjectsCreate = Eng.Agilium.Be.Features.Projects.Create;
using SprintsAssignItem = Eng.Agilium.Be.Features.Sprints.AssignItem;
using SprintsCreate = Eng.Agilium.Be.Features.Sprints.Create;

namespace Eng.Agilium.Be.Services;

/// <summary>
/// Optional demo data for local development (only in the Development environment and only with
/// <c>SEED_DEMO_DATA=true</c>): a ready-to-use user plus a small project, so the UI can be tried at once.
/// Idempotent - does nothing when the demo user already exists.
/// </summary>
public static class DevSeed
{
  public const string DemoEmail = "demo@example.com";
  public const string DemoPassword = "Demo-1234!";
  public const string DemoProjectTitle = "Demo project";

  public static bool IsEnabled(IConfiguration configuration, IHostEnvironment environment) =>
    environment.IsDevelopment()
    && string.Equals(configuration["SEED_DEMO_DATA"], "true", StringComparison.OrdinalIgnoreCase);

  public static async Task RunAsync(IServiceProvider services, CancellationToken cancellationToken = default)
  {
    using var scope = services.CreateScope();
    var sp = scope.ServiceProvider;
    var db = sp.GetRequiredService<AppDbContext>();

    if (await db.AppUsers.AnyAsync(u => u.Email == DemoEmail, cancellationToken))
      return;

    var user = new AppUser
    {
      Email = DemoEmail,
      Name = "Demo",
      Surname = "User",
      PasswordHash = BCrypt.Net.BCrypt.HashPassword(DemoPassword),
      IsActive = true,
    };
    db.AppUsers.Add(user);
    await db.SaveChangesAsync(cancellationToken);

    var projects = sp.GetRequiredService<ProjectsCreate.Handler>();
    projects.SetUpLoggedUser(new LoggedUser(user.Id, user.Email, false, []));
    var projectId = (
      await projects.HandleAsync(
        new ProjectsCreate.Command(DemoProjectTitle, "Sample data created by SEED_DEMO_DATA."),
        new EmptyParameters(),
        cancellationToken
      )
    ).Id;

    var items = sp.GetRequiredService<ItemsCreate.Handler>();
    async Task<int> Item(string title, ItemType type, int? parentId = null) =>
      (
        await items.HandleAsync(
          new ItemsCreate.Command(title, type, projectId, parentId),
          new EmptyParameters(),
          cancellationToken
        )
      ).Id;

    var feature = await Item("Sign-in", ItemType.Feature);
    var story = await Item("As a user I can sign in", ItemType.UserStory, feature);
    var task1 = await Item("Build the login form", ItemType.Task, story);
    var task2 = await Item("Validate credentials", ItemType.Task, story);
    await Item("Fix typo on the login page", ItemType.Bug, story);
    await Item("Write the project handbook", ItemType.Task); // lands in the generic containers

    var sprints = sp.GetRequiredService<SprintsCreate.Handler>();
    var sprintId = (
      await sprints.HandleAsync(
        new SprintsCreate.Command(projectId, "Sprint 1", DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(14)),
        new EmptyParameters(),
        cancellationToken
      )
    ).Id;

    var assign = sp.GetRequiredService<SprintsAssignItem.Handler>();
    foreach (var taskId in new[] { task1, task2 })
      await assign.HandleAsync(new SprintsAssignItem.Command(taskId), new IdParameters(sprintId), cancellationToken);
  }
}

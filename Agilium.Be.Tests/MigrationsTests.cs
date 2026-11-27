using Eng.Agilium.Be.Model.Db;
using Eng.Agilium.Be.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Testcontainers.MsSql;

namespace Eng.Agilium.Be.Tests;

/// <summary>Not part of the shared collection: uses its own empty database.</summary>
public class MigrationsTests : IAsyncLifetime
{
  private readonly MsSqlContainer container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

  public Task InitializeAsync() => container.StartAsync();

  public async Task DisposeAsync() => await container.DisposeAsync();

  private AppDbContext NewContext() =>
    new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(container.GetConnectionString()).Options);

  [Fact]
  public async Task Migrate_OnAnEmptyDatabase_Succeeds()
  {
    await using var db = NewContext();

    await db.Database.MigrateAsync();

    (await db.Database.GetPendingMigrationsAsync()).ShouldBeEmpty();
  }

  [Fact]
  public void Model_HasNoDriftFromTheMigrationSnapshot()
  {
    using var db = NewContext();
    var snapshot = db.GetService<IMigrationsAssembly>().ModelSnapshot.ShouldNotBeNull();
    var snapshotModel = db.GetService<IModelRuntimeInitializer>().Initialize(snapshot.Model);
    var differ = db.GetService<IMigrationsModelDiffer>();

    var differences = differ.GetDifferences(
      snapshotModel.GetRelationalModel(),
      db.GetService<IDesignTimeModel>().Model.GetRelationalModel()
    );

    differences.ShouldBeEmpty("a model change is missing a migration: " + string.Join(", ", differences.Select(d => d.GetType().Name)));
  }

  [Fact]
  public async Task Migration_MarksExistingUsersAsEmailVerified()
  {
    await using var db = NewContext();
    var migrator = db.GetService<IMigrator>();
    await migrator.MigrateAsync("20261126213152_Step3_Invitations");
    await db.Database.ExecuteSqlRawAsync(
      "INSERT INTO AppUsers (Email, Name, Surname, PasswordHash, IsActive, FailedLoginAttemptsCount) VALUES ('old@example.com', 'O', 'U', 'x', 1, 0)"
    );

    await migrator.MigrateAsync();

    var verifiedAt = await db.AppUsers.Where(u => u.Email == "old@example.com").Select(u => u.EmailVerifiedAt).SingleAsync();
    verifiedAt.ShouldNotBeNull();
  }

  [Fact]
  public async Task Migration_TurnsOldCommentsTextsIntoFirstComments()
  {
    await using var db = NewContext();
    var migrator = db.GetService<IMigrator>();
    await migrator.MigrateAsync("20261127125308_Step5_EmailVerification");
    // an owner, a project with a task whose Comments element holds a text, one whose text is blank, and a Description value
    await db.Database.ExecuteSqlRawAsync(@"
INSERT INTO AppUsers (Email, Name, Surname, PasswordHash, IsActive, FailedLoginAttemptsCount) VALUES ('o@example.com', 'O', 'W', 'x', 1, 0);
INSERT INTO Projects (Title, Description, State) VALUES ('P', '', 1);
INSERT INTO Roles (ProjectId, Title, SystemKind, CanViewProject, CanManageProject, CanViewMembers, CanManageMembers, CanManageSprints, CanEditItems) VALUES (1, 'Owner', 1, 1, 1, 1, 1, 1, 1);
INSERT INTO Memberships (ProjectId, UserId, RoleId) VALUES (1, 1, 1);
INSERT INTO Items (ProjectId, Title, Type, IsGeneric) VALUES (1, 'T1', 1, 0), (1, 'T2', 1, 0);
INSERT INTO Templates (ProjectId, Type) VALUES (1, 1);
INSERT INTO TemplateTables (TemplateId, OrderIndex) VALUES (1, 1);
INSERT INTO TemplateColumns (TemplateTableId, Width, OrderIndex) VALUES (1, 12, 1);
INSERT INTO TemplateSections (TemplateColumnId, Title, OrderIndex) VALUES (1, 'S', 1);
INSERT INTO TemplateItems (TemplateSectionId, [Key], Title, OrderIndex, [Type]) VALUES (1, 'comments', 'Comments', 1, 6), (1, 'description', 'Description', 2, 2);
INSERT INTO ItemFieldValues (ItemId, TemplateItemId, [Value]) VALUES (1, 1, 'old **text**'), (2, 1, '  '), (1, 2, 'keep me');
");

    await migrator.MigrateAsync();

    var comments = await db.ItemComments.Select(c => new { c.ItemId, c.TemplateItemId, c.AuthorId, c.Text }).ToListAsync();
    comments.Count.ShouldBe(1);
    comments[0].ItemId.ShouldBe(1);
    comments[0].AuthorId.ShouldBe(1);
    comments[0].Text.ShouldBe("old **text**");
    var remaining = await db.ItemFieldValues.Select(v => v.Value).ToListAsync();
    remaining.ShouldBe(["keep me"]);
  }
}

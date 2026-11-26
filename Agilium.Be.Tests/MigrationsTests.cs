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
}

using Eng.Agilium.Be.Features.Templates;
using Eng.Agilium.Be.Model.Db;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Respawn;

namespace Eng.Agilium.Be.Tests.Infrastructure;

/// <summary>
/// The whole backend hosted in memory against the SQL Server container. Settings are passed through
/// <c>UseSetting</c> because <c>Program</c> reads the configuration while the builder is still being set up.
/// </summary>
public sealed class ApiFactory(SqlServerFixture sql) : WebApplicationFactory<Program>
{
  public const string JwtKey = "test-jwt-key-test-jwt-key-test-jwt-key-test-jwt-key-0123456789";

  private Respawner? respawner;

  public SqlServerFixture Sql { get; } = sql;

  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    builder.UseEnvironment("Testing");
    builder.UseSetting("ConnectionStrings:DefaultConnection", Sql.ConnectionString);
    builder.UseSetting("AppSettings:UseSwagger", "false");
    builder.UseSetting("AppSettings:Security:Jwt:Key", JwtKey);
    builder.UseSetting("AppSettings:Security:Turnstile:Enabled", "false");
    builder.UseSetting("AppSettings:Security:RequestLimiter:PermitLimit", "1000000");
    builder.UseSetting("AppSettings:Security:RequestLimiter:QueueLimit", "1000");
    builder.UseSetting("AppSettings:Email:Smtp:Host", "localhost");
    builder.UseSetting("AppSettings:Email:Smtp:Username", "test");
    builder.UseSetting("AppSettings:Email:Smtp:Password", "test");
  }

  /// <summary>Runs <paramref name="action"/> with a fresh <see cref="AppDbContext"/> (direct DB access for asserts/arrange).</summary>
  public async Task WithDbAsync(Func<AppDbContext, Task> action)
  {
    using var scope = Services.CreateScope();
    await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
  }

  public async Task<T> WithDbAsync<T>(Func<AppDbContext, Task<T>> action)
  {
    using var scope = Services.CreateScope();
    return await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
  }

  /// <summary>Wipes all data (except migration history) and re-seeds the global templates.</summary>
  public async Task ResetDatabaseAsync()
  {
    _ = Server; // makes sure the app (and its migration on startup) is running
    await using var conn = new SqlConnection(Sql.ConnectionString);
    await conn.OpenAsync();
    respawner ??= await Respawner.CreateAsync(
      conn,
      new RespawnerOptions { TablesToIgnore = [new Respawn.Graph.Table("__EFMigrationsHistory")] }
    );
    await respawner.ResetAsync(conn);

    using var scope = Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    DefaultTemplates.EnsureGlobalTemplates(db);
    await db.SaveChangesAsync();
  }
}

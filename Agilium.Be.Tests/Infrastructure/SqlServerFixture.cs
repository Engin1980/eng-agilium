using Testcontainers.MsSql;

namespace Eng.Agilium.Be.Tests.Infrastructure;

/// <summary>One real SQL Server container for the whole test run (the code relies on SQL Server specifics).</summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
  private readonly MsSqlContainer container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

  public string ConnectionString => container.GetConnectionString();

  public Task InitializeAsync() => container.StartAsync();

  public async Task DisposeAsync() => await container.DisposeAsync();
}

namespace Eng.Agilium.Be.Tests.Infrastructure;

/// <summary>All integration tests share one container/app and run sequentially (they share one database).</summary>
[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<ApiFixture>
{
  public const string Name = "Api";
}

public sealed class ApiFixture : IAsyncLifetime
{
  private readonly SqlServerFixture sql = new();

  public ApiFactory Factory { get; private set; } = null!;

  public async Task InitializeAsync()
  {
    await sql.InitializeAsync();
    Factory = new ApiFactory(sql);
  }

  public async Task DisposeAsync()
  {
    await Factory.DisposeAsync();
    await sql.DisposeAsync();
  }
}

/// <summary>Base class of integration tests: every test starts with a clean database.</summary>
[Collection(ApiCollection.Name)]
public abstract class ApiTestBase(ApiFixture fixture) : IAsyncLifetime
{
  protected ApiFactory Factory { get; } = fixture.Factory;

  protected TestApi Api { get; private set; } = null!;

  public virtual async Task InitializeAsync()
  {
    await Factory.ResetDatabaseAsync();
    Api = new TestApi(Factory);
  }

  public virtual Task DisposeAsync() => Task.CompletedTask;
}

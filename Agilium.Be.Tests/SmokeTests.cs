using Eng.Agilium.Be.Model.Db;
using Eng.Agilium.Be.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Eng.Agilium.Be.Tests;

public class SmokeTests(ApiFixture fixture) : ApiTestBase(fixture)
{
  [Fact]
  public async Task Ping_Anonymous_ReturnsOk()
  {
    var response = await Api.Get("health/ping");
    response.Code.ShouldBe(204);
  }

  [Fact]
  public void App_UsesTheContainerDatabase_NotAnyLocalOne()
  {
    using var scope = Factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.GetConnectionString().ShouldBe(Factory.Sql.ConnectionString);
  }

  [Fact]
  public void Database_AfterReset_ContainsGlobalTemplates()
  {
    using var scope = Factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Templates.Count(t => t.ProjectId == null).ShouldBe(Enum.GetValues<ItemType>().Length);
  }
}

using Eng.Agilium.Be.Model.Db;
using Eng.Agilium.Be.Services;
using Eng.Agilium.Be.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Eng.Agilium.Be.Tests;

public class DevSeedTests(ApiFixture fixture) : ApiTestBase(fixture)
{
  [Fact]
  public async Task Run_CreatesADemoUserWhoCanLogInAndAProjectWithContent()
  {
    await DevSeed.RunAsync(Factory.Services);

    (await Api.LoginRawAsync(DevSeed.DemoEmail, DevSeed.DemoPassword)).Code.ShouldBe(200);
    await Factory.WithDbAsync(async db =>
    {
      var project = await db.Projects.SingleAsync(p => p.Title == DevSeed.DemoProjectTitle);
      (await db.Items.CountAsync(i => i.ProjectId == project.Id)).ShouldBeGreaterThan(3);
      (await db.Sprints.CountAsync(s => s.ProjectId == project.Id)).ShouldBe(1);
      (await db.SprintItems.CountAsync()).ShouldBe(2);
    });
  }

  [Fact]
  public async Task Run_Twice_DoesNotDuplicateAnything()
  {
    await DevSeed.RunAsync(Factory.Services);
    await DevSeed.RunAsync(Factory.Services);

    await Factory.WithDbAsync(async db =>
    {
      (await db.AppUsers.CountAsync(u => u.Email == DevSeed.DemoEmail)).ShouldBe(1);
      (await db.Projects.CountAsync(p => p.Title == DevSeed.DemoProjectTitle)).ShouldBe(1);
    });
  }
}

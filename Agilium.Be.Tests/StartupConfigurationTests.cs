using Eng.Agilium.Be.Tests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;

namespace Eng.Agilium.Be.Tests;

/// <summary>Starting the real application with the committed defaults outside development must fail fast.</summary>
public class StartupConfigurationTests
{
  [Fact]
  public void Start_InProductionWithTheDefaultConfiguration_Fails()
  {
    using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
      builder.UseEnvironment("Production")
    );

    var ex = Should.Throw<Exception>(() => factory.CreateClient());

    ex.ToString().ShouldContain("Invalid configuration");
  }
}

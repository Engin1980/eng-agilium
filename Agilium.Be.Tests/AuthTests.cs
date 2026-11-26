using Eng.Agilium.Be.Model.Db;
using Eng.Agilium.Be.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Eng.Agilium.Be.Tests;

public class AuthTests(ApiFixture fixture) : ApiTestBase(fixture)
{
  private object NewUser(string email, string password = TestApi.DefaultPassword) =>
    new
    {
      email,
      name = "N",
      surname = "S",
      password,
    };

  [Fact]
  public async Task Register_ShortPassword_Returns400()
  {
    var response = await Api.Post("auth/users", NewUser("a@example.com", "short"));
    response.Code.ShouldBe(400);
    response.ErrorKey.ShouldBe("COMMAND_VALIDATION_EXCEPTION");
  }

  [Fact]
  public async Task Register_DuplicateEmail_Returns409()
  {
    (await Api.Post("auth/users", NewUser("dup@example.com"))).Code.ShouldBe(201);
    var response = await Api.Post("auth/users", NewUser("DUP@example.com"));
    response.Code.ShouldBe(409);
    response.ErrorKey.ShouldBe("ENTITY_ALREADY_EXISTS");
  }

  [Fact]
  public async Task Login_EmailIsCaseInsensitive()
  {
    await Api.RegisterAsync("case@example.com");
    (await Api.LoginRawAsync("CASE@Example.com")).Code.ShouldBe(200);
  }

  [Fact]
  public async Task Login_WrongPasswordAndUnknownEmail_LookTheSame()
  {
    await Api.RegisterAsync("known@example.com");
    var wrongPassword = await Api.LoginRawAsync("known@example.com", "bad-password");
    var unknown = await Api.LoginRawAsync("unknown@example.com");

    wrongPassword.Code.ShouldBe(401);
    unknown.Code.ShouldBe(401);
    wrongPassword.ErrorKey.ShouldBe("AUTHENTICATION_FAILED");
    unknown.Json.GetProperty("params")[0].GetString().ShouldBe(wrongPassword.Json.GetProperty("params")[0].GetString());
  }

  [Fact]
  public async Task Login_AfterMaxFailedAttempts_AccountIsLocked()
  {
    await Api.RegisterAsync("lock@example.com");
    for (var i = 0; i < 5; i++)
      (await Api.LoginRawAsync("lock@example.com", "bad-password")).Code.ShouldBe(401);

    var response = await Api.LoginRawAsync("lock@example.com");
    response.Code.ShouldBe(400);
    response.ErrorKey.ShouldBe("ACCOUNT_LOCKED");
  }

  [Fact]
  public async Task Login_DeactivatedAccount_Returns400()
  {
    var id = await Api.RegisterAsync("off@example.com");
    using (var scope = Factory.Services.CreateScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
      await db.AppUsers.Where(u => u.Id == id).ExecuteUpdateAsync(s => s.SetProperty(u => u.IsActive, false));
    }

    var response = await Api.LoginRawAsync("off@example.com");
    response.Code.ShouldBe(400);
    response.ErrorKey.ShouldBe("ACCOUNT_DEACTIVATED");
  }

  [Fact]
  public async Task Login_ReturnsJwtAndRefreshCookie()
  {
    await Api.RegisterAsync("jwt@example.com");
    var response = await Api.LoginRawAsync("jwt@example.com");

    response.Code.ShouldBe(200);
    response.As<string>().Split('.').Length.ShouldBe(3);
    TestApi.RefreshCookieFrom(response).ShouldNotBeNull();
  }

  [Fact]
  public async Task Refresh_RotatesTheRefreshToken()
  {
    var user = await Api.RegisterAndLoginAsync();
    var first = user.RefreshCookie!;

    var refreshed = await Api.Send(HttpMethod.Post, "auth/refresh", new { }, first);
    refreshed.Code.ShouldBe(200);
    var second = TestApi.RefreshCookieFrom(refreshed);
    second.ShouldNotBeNull();
    second.ShouldNotBe(first);

    // the old token is single-use
    (await Api.Send(HttpMethod.Post, "auth/refresh", new { }, first)).Code.ShouldBe(401);
    (await Api.Send(HttpMethod.Post, "auth/refresh", new { }, second)).Code.ShouldBe(200);
  }

  [Fact]
  public async Task Refresh_WithoutCookie_Returns401() =>
    (await Api.Post("auth/refresh")).Code.ShouldBe(401);

  [Fact]
  public async Task Logout_RevokesTheRefreshToken()
  {
    var user = await Api.RegisterAndLoginAsync();

    // like the browser: the token travels in the httpOnly cookie, the body is empty
    (await Api.Send(HttpMethod.Post, "auth/logout", new { refreshToken = "" }, user.RefreshCookie)).Code.ShouldBe(204);

    (await Api.Send(HttpMethod.Post, "auth/refresh", new { }, user.RefreshCookie)).Code.ShouldBe(401);
  }
}

using Eng.Agilium.Be.Model.Db;
using Eng.Agilium.Be.Services;
using Eng.Agilium.Be.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Eng.Agilium.Be.Tests;

public class AccountTests(ApiFixture fixture) : ApiTestBase(fixture)
{
  private static string NewEmail() => $"acc-{Guid.NewGuid():N}@example.com";

  private Task<ApiResponse> Register(string email, string password = TestApi.DefaultPassword) =>
    Api.Post("auth/users", new { email, name = "N", surname = "S", password });

  private string TokenFor(string email) => FakeEmailSender.TokenOf(Factory.Emails.To(email).Last());

  private Task<AppUser> UserAsync(string email) =>
    Factory.WithDbAsync(db => db.AppUsers.AsNoTracking().SingleAsync(u => u.Email == email));

  private Task ExpireTokensAsync(TokenType type) =>
    Factory.WithDbAsync(db =>
      db.Tokens.Where(t => t.Type == type).ExecuteUpdateAsync(s => s.SetProperty(t => t.Expiration, DateTime.UtcNow.AddMinutes(-1)))
    );

  // ---- registration & verification

  [Fact]
  public async Task Register_SendsExactlyOneEmail_WithAValidLink_AndTheAccountCannotLogInYet()
  {
    var email = NewEmail();
    (await Register(email)).Code.ShouldBe(204);

    var mails = Factory.Emails.To(email);
    mails.Count.ShouldBe(1);
    mails[0].TextBody.ShouldContain("/verify-email?token=");
    mails[0].HtmlBody.ShouldContain("/verify-email?token=");
    (await UserAsync(email)).EmailVerifiedAt.ShouldBeNull();

    var login = await Api.LoginRawAsync(email);
    login.Code.ShouldBe(400);
    login.ErrorKey.ShouldBe("EMAIL_NOT_VERIFIED");
  }

  [Fact]
  public async Task Login_WithWrongPassword_DoesNotRevealTheUnverifiedState()
  {
    var email = NewEmail();
    await Register(email);

    (await Api.LoginRawAsync(email, "wrong-password")).Code.ShouldBe(401);
  }

  [Fact]
  public async Task VerifyEmail_AllowsLogin_AndTheTokenIsSingleUse()
  {
    var email = NewEmail();
    await Register(email);
    var token = TokenFor(email);

    (await Api.Post("auth/verify-email", new { token })).Code.ShouldBe(204);
    (await Api.LoginRawAsync(email)).Code.ShouldBe(200);

    var again = await Api.Post("auth/verify-email", new { token });
    again.Code.ShouldBe(400);
    again.ErrorKey.ShouldBe("INVALID_TOKEN");
  }

  [Fact]
  public async Task VerifyEmail_UnknownOrExpiredToken_Is400_WithTheSameAnswer()
  {
    var email = NewEmail();
    await Register(email);
    var token = TokenFor(email);
    await ExpireTokensAsync(TokenType.EmailVerification);

    var expired = await Api.Post("auth/verify-email", new { token });
    var unknown = await Api.Post("auth/verify-email", new { token = "does-not-exist" });

    expired.Code.ShouldBe(400);
    unknown.Code.ShouldBe(400);
    expired.Body.ShouldBe(unknown.Body);
    (await UserAsync(email)).EmailVerifiedAt.ShouldBeNull();
  }

  [Fact]
  public async Task VerifyEmail_TokenIsStoredOnlyAsAHash_AndExpiresAfter24Hours()
  {
    var email = NewEmail();
    await Register(email);
    var token = TokenFor(email);

    var stored = await Factory.WithDbAsync(db => db.Tokens.SingleAsync(t => t.Type == TokenType.EmailVerification));
    stored.Value.ShouldNotBe(token);
    (stored.Expiration - DateTime.UtcNow).ShouldBeInRange(TimeSpan.FromHours(23.9), TimeSpan.FromHours(24.1));
  }

  [Fact]
  public async Task Register_ExistingEmail_GivesTheSameAnswer_AndNoSecondAccount()
  {
    var email = NewEmail();
    var first = await Register(email);
    Factory.Emails.Clear();

    var second = await Register(email);

    second.Code.ShouldBe(first.Code);
    second.Body.ShouldBe(first.Body);
    (await Factory.WithDbAsync(db => db.AppUsers.CountAsync(u => u.Email == email))).ShouldBe(1);
    var mails = Factory.Emails.To(email);
    mails.Count.ShouldBe(1);
    mails[0].TextBody.ShouldNotContain("token=");
    (await Factory.WithDbAsync(db => db.Tokens.CountAsync(t => t.Type == TokenType.EmailVerification))).ShouldBe(1);
  }

  [Fact]
  public async Task Register_WhenSmtpFails_StillAnswers204_AndResendWorksLater()
  {
    var email = NewEmail();
    Factory.Emails.Fail = true;
    try
    {
      (await Register(email)).Code.ShouldBe(204);
    }
    finally
    {
      Factory.Emails.Fail = false;
    }
    Factory.Emails.To(email).ShouldBeEmpty();

    (await Api.Post("auth/verify-email/resend", new { email })).Code.ShouldBe(204);
    Factory.Emails.To(email).Count.ShouldBe(1);
  }

  [Fact]
  public async Task Resend_IsLimitedToOnePerMinutePerAccount_AndAlwaysAnswers204()
  {
    var email = NewEmail();
    await Register(email); // the registration itself does not use the resend limit
    Factory.Emails.Clear();

    (await Api.Post("auth/verify-email/resend", new { email })).Code.ShouldBe(204);
    (await Api.Post("auth/verify-email/resend", new { email })).Code.ShouldBe(204);

    Factory.Emails.To(email).Count.ShouldBe(1);
    // the new token replaced the old one
    (await Factory.WithDbAsync(db => db.Tokens.CountAsync(t => t.Type == TokenType.EmailVerification))).ShouldBe(1);
  }

  [Fact]
  public async Task Resend_ForUnknownOrVerifiedAccounts_SendsNothing_AndLooksTheSame()
  {
    var verified = NewEmail();
    await Api.RegisterAsync(verified);
    Factory.Emails.Clear();

    var unknown = await Api.Post("auth/verify-email/resend", new { email = NewEmail() });
    var known = await Api.Post("auth/verify-email/resend", new { email = verified });

    unknown.Code.ShouldBe(204);
    known.Code.ShouldBe(204);
    known.Body.ShouldBe(unknown.Body);
    Factory.Emails.Sent.ShouldBeEmpty();
  }

  // ---- password reset

  [Fact]
  public async Task PasswordReset_UnknownEmail_Is204_AndSendsNothing()
  {
    var response = await Api.Post("auth/password-reset/request", new { email = NewEmail() });

    response.Code.ShouldBe(204);
    Factory.Emails.Sent.ShouldBeEmpty();
  }

  [Fact]
  public async Task PasswordReset_FullFlow_NewPasswordWorks_OldSessionsAreRevoked_TokenIsSingleUse()
  {
    var user = await Api.RegisterAndLoginAsync();
    var oldRefresh = user.RefreshCookie!;
    Factory.Emails.Clear();

    (await Api.Post("auth/password-reset/request", new { email = user.Email })).Code.ShouldBe(204);
    var mail = Factory.Emails.To(user.Email).Single();
    mail.TextBody.ShouldContain("/reset-password?token=");
    var token = FakeEmailSender.TokenOf(mail);

    (await Api.Post("auth/password-reset/confirm", new { token, newPassword = "New-password-1" })).Code.ShouldBe(204);

    (await Api.LoginRawAsync(user.Email)).Code.ShouldBe(401);
    (await Api.LoginRawAsync(user.Email, "New-password-1")).Code.ShouldBe(200);
    (await Api.Send(HttpMethod.Post, "auth/refresh", new { }, oldRefresh)).Code.ShouldBe(401);

    var again = await Api.Post("auth/password-reset/confirm", new { token, newPassword = "Another-password-1" });
    again.Code.ShouldBe(400);
    again.ErrorKey.ShouldBe("INVALID_TOKEN");
  }

  [Fact]
  public async Task PasswordReset_UnlocksTheAccount_AndConfirmsTheEmail()
  {
    var email = NewEmail();
    await Register(email);
    for (var i = 0; i < 6; i++)
      await Api.LoginRawAsync(email, "wrong-password");
    (await UserAsync(email)).FailedLoginAttemptsCount.ShouldBeGreaterThan(0);
    await Api.Post("auth/password-reset/request", new { email });
    var token = TokenFor(email);

    (await Api.Post("auth/password-reset/confirm", new { token, newPassword = "New-password-1" })).Code.ShouldBe(204);

    var user = await UserAsync(email);
    user.FailedLoginAttemptsCount.ShouldBe(0);
    user.EmailVerifiedAt.ShouldNotBeNull();
    (await Api.LoginRawAsync(email, "New-password-1")).Code.ShouldBe(200);
  }

  [Fact]
  public async Task PasswordReset_ExpiredToken_AndShortPassword_AreRejected()
  {
    var user = await Api.RegisterAndLoginAsync();
    await Api.Post("auth/password-reset/request", new { email = user.Email });
    var token = TokenFor(user.Email);

    (await Api.Post("auth/password-reset/confirm", new { token, newPassword = "short" })).Code.ShouldBe(400);
    await ExpireTokensAsync(TokenType.PasswordReset);

    var expired = await Api.Post("auth/password-reset/confirm", new { token, newPassword = "New-password-1" });
    expired.Code.ShouldBe(400);
    expired.ErrorKey.ShouldBe("INVALID_TOKEN");
    (await Api.LoginRawAsync(user.Email)).Code.ShouldBe(200);
  }

  [Fact]
  public async Task PasswordReset_TokenOfAnotherType_DoesNotWork()
  {
    var email = NewEmail();
    await Register(email);
    var verificationToken = TokenFor(email);

    var response = await Api.Post("auth/password-reset/confirm", new { token = verificationToken, newPassword = "New-password-1" });

    response.Code.ShouldBe(400);
  }

  [Fact]
  public async Task PasswordReset_Request_IsLimitedPerAccount()
  {
    var user = await Api.RegisterAndLoginAsync();
    Factory.Emails.Clear();

    await Api.Post("auth/password-reset/request", new { email = user.Email });
    await Api.Post("auth/password-reset/request", new { email = user.Email });

    Factory.Emails.To(user.Email).Count.ShouldBe(1);
  }

  // ---- change password

  [Fact]
  public async Task ChangePassword_WrongCurrentPassword_Is400_AndTheSessionStays()
  {
    var user = await Api.RegisterAndLoginAsync();

    var response = await user.Post("auth/password", new { currentPassword = "wrong", newPassword = "New-password-1" });

    response.Code.ShouldBe(400);
    (await user.Get("projects")).Code.ShouldBe(200);
    (await Api.LoginRawAsync(user.Email)).Code.ShouldBe(200);
  }

  [Fact]
  public async Task ChangePassword_Works_KeepsThisSession_AndSignsOutTheOthers()
  {
    var user = await Api.RegisterAndLoginAsync();
    var other = await Api.LoginRawAsync(user.Email);
    var otherRefresh = TestApi.RefreshCookieFrom(other)!;

    var response = await user.Send(
      HttpMethod.Post,
      "auth/password",
      new { currentPassword = TestApi.DefaultPassword, newPassword = "New-password-1" },
      user.RefreshCookie
    );

    response.Code.ShouldBe(204);
    (await Api.LoginRawAsync(user.Email)).Code.ShouldBe(401);
    (await Api.LoginRawAsync(user.Email, "New-password-1")).Code.ShouldBe(200);
    (await Api.Send(HttpMethod.Post, "auth/refresh", new { }, otherRefresh)).Code.ShouldBe(401);
    (await Api.Send(HttpMethod.Post, "auth/refresh", new { }, user.RefreshCookie)).Code.ShouldBe(200);
  }

  [Fact]
  public async Task ChangePassword_RequiresLogin()
  {
    (await Api.Post("auth/password", new { currentPassword = "a", newPassword = "New-password-1" })).Code.ShouldBe(401);
  }

  // ---- seed

  [Fact]
  public async Task SeededDemoUser_IsVerified()
  {
    await DevSeed.RunAsync(Factory.Services);

    (await UserAsync(DevSeed.DemoEmail)).EmailVerifiedAt.ShouldNotBeNull();
  }
}

public class DemoSeedInvitationTests(ApiFixture fixture) : ApiTestBase(fixture)
{
  [Fact]
  public async Task DemoInvitationKey_JoinsTheDemoProjectAsMember()
  {
    await DevSeed.RunAsync(Factory.Services);
    var user = await Api.RegisterAndLoginAsync();

    var response = await user.Post("projects/join", new { key = DevSeed.DemoInvitationKey });

    response.Code.ShouldBe(200, response.Body);
    (await user.Get("projects")).Json.GetProperty("projects").GetArrayLength().ShouldBe(1);
  }
}

using System.Security.Cryptography;
using Eng.Agilium.Be.Exceptions;
using Eng.Agilium.Be.Model.Db;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Crypto.Modes;

namespace Eng.Agilium.Be.Services;

public class TokenService(AppDbContext dbContext, AppSettingsService appSettingsService)
{
  private static readonly System.Threading.SemaphoreSlim revokeExpiredLock = new(1, 1);
  private readonly TokensSettings tokensSettings = appSettingsService.AppSettings.Security.Tokens;

  public async Task<AppUser> ObtainAppUserByTokenAsync(string tokenValue, TokenType tokenType, bool deleteExistingToken)
  {
    string tokenHash = CalculateHash(tokenValue);
    var token =
      await dbContext
        .Tokens.Include(q => q.AppUser)
          .ThenInclude(q => q.Memberships)
        .FirstOrDefaultAsync(t => t.Value == tokenHash && t.Type == tokenType)
      ?? throw new AuthenticationFailedException(AuthenticationFailedException.FailureReason.TokenNotFound, null);
    if (token.Expiration < DateTime.UtcNow)
      throw new AuthenticationFailedException(AuthenticationFailedException.FailureReason.TokenExpired, token?.AppUser);
    if (deleteExistingToken)
    {
      dbContext.Tokens.Remove(token);
      await dbContext.SaveChangesAsync();
    }

    await RevokeExpiredAsync();

    return token.AppUser;
  }

  public async Task<string> CreatePasswordResetAsync(AppUser appUser)
  {
    return await CreateAsync(
      appUser.Id,
      TokenType.PasswordReset,
      tokensSettings.PasswordResetTokenLength,
      TimeSpan.FromMinutes(tokensSettings.PasswordResetTokenExpirationMinutes),
      true
    );
  }

  public async Task<string> CreateRefreshAsync(AppUser appUser)
  {
    return await CreateAsync(
      appUser.Id,
      TokenType.Refresh,
      tokensSettings.RefreshTokenLength,
      TimeSpan.FromMinutes(tokensSettings.RefreshTokenExpirationMinutes),
      false
    );
  }

  private async Task<string> CreateAsync(
    int appUserId,
    TokenType tokenType,
    int length,
    TimeSpan expiration,
    bool deleteExistingTokens
  )
  {
    string tokenString = GenerateRandomTokenString(length);
    string hashedTokenString = CalculateHash(tokenString);
    var token = new Token
    {
      AppUserId = appUserId,
      Type = tokenType,
      Value = hashedTokenString,
      Expiration = DateTime.UtcNow.Add(expiration),
    };

    if (deleteExistingTokens)
    {
      var existingTokens = await dbContext
        .Tokens.Where(t => t.AppUserId == appUserId && t.Type == tokenType)
        .ToListAsync();
      dbContext.Tokens.RemoveRange(existingTokens);
    }

    await dbContext.Tokens.AddAsync(token);
    await dbContext.SaveChangesAsync(); // todo use here cancellation tokens in general in this service ??
    return tokenString;
  }

  public async Task RevokeRefreshTokenAsync(string refreshToken)
  {
    var tokenHash = CalculateHash(refreshToken);
    var token = await dbContext.Tokens.FirstOrDefaultAsync(t => t.Value == tokenHash && t.Type == TokenType.Refresh);
    if (token != null)
    {
      dbContext.Tokens.Remove(token);
      await dbContext.SaveChangesAsync();
    }
  }

  private static string CalculateHash(string input)
  {
    byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(input);
    byte[] hashBytes = SHA512.HashData(inputBytes);
    return Convert.ToBase64String(hashBytes);
  }

  private static string GenerateRandomTokenString(int length)
  {
    byte[] bytes = RandomNumberGenerator.GetBytes(length);
    string base64Token = Convert.ToBase64String(bytes).Replace("/", "_").Replace("+", "-");
    return base64Token;
  }

  private async Task RevokeExpiredAsync()
  {
    if (!await revokeExpiredLock.WaitAsync(0))
      return;
    try
    {
      var now = DateTime.UtcNow;
      await dbContext.Tokens.Where(t => t.Expiration < now).ExecuteDeleteAsync();
      dbContext.ChangeTracker.Clear();
    }
    finally
    {
      revokeExpiredLock.Release();
    }
  }
}

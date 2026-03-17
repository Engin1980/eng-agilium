using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Eng.Agilium.Be.Features;

public class Utils
{
  public static string GenerateJwtToken(
    int id,
    string? email,
    string? name,
    string? surname,
    string role,
    string keyString,
    string issuer,
    int expirationMinutes
  )
  {
    List<Claim> claims = [new Claim("sub", id.ToString()), new Claim("role", role)];
    if (email != null)
      claims.Add(new Claim("email", email));
    if (name != null)
      claims.Add(new Claim("name", name));
    if (surname != null)
      claims.Add(new Claim("surname", surname));

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var token = new JwtSecurityToken(
      issuer: issuer,
      audience: null,
      claims: [.. claims],
      expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
      signingCredentials: creds
    );
    var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
    return tokenString;
  }

  public static void AppendRefreshTokenCookie(string refreshToken, int expirationMinutes, HttpContext httpContext)
  {
    httpContext.Response.Cookies.Append(
      "refreshToken",
      refreshToken,
      new CookieOptions
      {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
      }
    );
  }
}

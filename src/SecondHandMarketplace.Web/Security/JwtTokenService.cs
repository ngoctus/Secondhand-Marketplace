using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SecondHandMarketplace.Web.Security;

public sealed class JwtTokenService(IConfiguration configuration)
{
    public (string Token, DateTime ExpiresAtUtc) CreateToken(int userId, string email, string fullName, string role, string? securityStamp = null)
    {
        var section = configuration.GetSection("Jwt");
        var key = section["Key"] ?? throw new InvalidOperationException("Missing Jwt:Key configuration.");
        if (Encoding.UTF8.GetByteCount(key) < 32)
            throw new InvalidOperationException("Jwt:Key must be at least 32 bytes.");

        var issuer = section["Issuer"] ?? "SecondHandMarketplace";
        var audience = section["Audience"] ?? "SecondHandMarketplace.Client";
        var expires = DateTime.UtcNow.AddMinutes(int.TryParse(section["ExpiresMinutes"], out var minutes) && minutes > 0 ? minutes : 60);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, fullName),
            new(ClaimTypes.Role, role)
        };
        if (!string.IsNullOrEmpty(securityStamp))
        {
            claims.Add(new("sec_stamp", securityStamp));
        }
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(issuer, audience, claims, expires: expires, signingCredentials: credentials);
        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}

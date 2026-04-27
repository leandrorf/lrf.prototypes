using Microsoft.IdentityModel.Tokens;
using lrfdev.auth.api.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace lrfdev.auth.api.Services;

public sealed class TokenService(IConfiguration configuration) : ITokenService
{
    public string GenerateAccessToken(User user, IReadOnlyCollection<string> permissions)
    {
        var key = configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("JWT signing key is missing.");
        var issuer = configuration["Jwt:Issuer"] ?? "lrfdev.auth.api";
        var audience = configuration["Jwt:Audience"] ?? "lrf.resource.api";
        var expiresMinutes = int.TryParse(configuration["Jwt:AccessTokenMinutes"], out var parsedValue)
            ? parsedValue
            : 60;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new(JwtRegisteredClaimNames.Email, user.Email)
        };

        claims.AddRange(permissions.Select(permission => new Claim("permission", permission)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateClientAccessToken(string clientId, IReadOnlyCollection<string> scopes)
    {
        var key = configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("JWT signing key is missing.");
        var issuer = configuration["Jwt:Issuer"] ?? "lrfdev.auth.api";
        var audience = configuration["Jwt:Audience"] ?? "lrf.resource.api";
        var expiresMinutes = int.TryParse(configuration["Jwt:AccessTokenMinutes"], out var parsedValue)
            ? parsedValue
            : 60;

        var scopeValue = string.Join(' ', scopes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, $"client:{clientId}"),
            new("client_id", clientId),
            new("scope", scopeValue)
        };

        claims.AddRange(scopes.Select(scope => new Claim("permission", scope)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

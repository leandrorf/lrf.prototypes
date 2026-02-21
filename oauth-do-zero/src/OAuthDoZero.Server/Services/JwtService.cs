using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using OAuthDoZero.Server.Models;
using OAuthDoZero.Server.DTOs;

namespace OAuthDoZero.Server.Services;

/// <summary>
/// Serviço para geração e validação de tokens JWT
/// </summary>
public interface IJwtService
{
    string GenerateAccessToken(User? user, Client client, IEnumerable<string> scopes, TimeSpan expiry);
    string GenerateIdToken(User user, Client client, IEnumerable<string> scopes, string nonce, TimeSpan expiry);
    ClaimsPrincipal? ValidateToken(string token);
    string GenerateJti();
}

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;
    private readonly ICryptographyService _cryptoService;

    public JwtService(IConfiguration configuration, ICryptographyService cryptoService)
    {
        _configuration = configuration;
        _cryptoService = cryptoService;
    }

    public string GenerateAccessToken(User? user, Client client, IEnumerable<string> scopes, TimeSpan expiry)
    {
        var jti = GenerateJti();
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, jti),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new("client_id", client.ClientId),
            new("scope", string.Join(" ", scopes))
        };

        // Para machine-to-machine, usar client_id como subject
        if (user == null)
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, client.ClientId));
            claims.Add(new Claim("client_type", "machine"));
        }
        else
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, user.Id));
            claims.Add(new Claim("client_type", "user"));
            
            // Adicionar claims baseados nos scopes apenas se houver usuário
            AddScopeClaims(claims, user, scopes);
        }

        return GenerateToken(claims, expiry, client.ClientId);
    }

    public string GenerateIdToken(User user, Client client, IEnumerable<string> scopes, string nonce, TimeSpan expiry)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Aud, client.ClientId),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new("auth_time", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        if (!string.IsNullOrEmpty(nonce))
            claims.Add(new Claim(JwtRegisteredClaimNames.Nonce, nonce));

        // Adicionar claims do OpenID Connect
        AddScopeClaims(claims, user, scopes);

        return GenerateToken(claims, expiry, client.ClientId);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = GetTokenValidationParameters();

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    public string GenerateJti()
    {
        return Guid.NewGuid().ToString();
    }

    private void AddScopeClaims(List<Claim> claims, User user, IEnumerable<string> scopes)
    {
        foreach (var scope in scopes)
        {
            switch (scope)
            {
                case "profile":
                    if (!string.IsNullOrEmpty(user.FirstName))
                        claims.Add(new Claim("given_name", user.FirstName));
                    if (!string.IsNullOrEmpty(user.LastName))
                        claims.Add(new Claim("family_name", user.LastName));
                    if (!string.IsNullOrEmpty(user.PreferredUsername))
                        claims.Add(new Claim("preferred_username", user.PreferredUsername));
                    if (!string.IsNullOrEmpty(user.Picture))
                        claims.Add(new Claim("picture", user.Picture));
                    if (!string.IsNullOrEmpty(user.Locale))
                        claims.Add(new Claim("locale", user.Locale));
                    if (!string.IsNullOrEmpty(user.TimeZone))
                        claims.Add(new Claim("zoneinfo", user.TimeZone));

                    var fullName = $"{user.FirstName} {user.LastName}".Trim();
                    if (!string.IsNullOrEmpty(fullName))
                        claims.Add(new Claim("name", fullName));
                    break;

                case "email":
                    claims.Add(new Claim("email", user.Email));
                    claims.Add(new Claim("email_verified", user.EmailConfirmed.ToString().ToLower()));
                    break;
            }
        }
    }

    private string GenerateToken(List<Claim> claims, TimeSpan expiry, string audience)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetJwtSecret()));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: GetJwtIssuer(),
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(expiry),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private TokenValidationParameters GetTokenValidationParameters()
    {
        return new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetJwtSecret())),
            ValidateIssuer = true,
            ValidIssuer = GetJwtIssuer(),
            ValidateAudience = false, // Validamos pelo client_id
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5)
        };
    }

    private string GetJwtSecret()
    {
        return _configuration["JWT:Secret"] ?? throw new InvalidOperationException("JWT:Secret not configured");
    }

    private string GetJwtIssuer()
    {
        return _configuration["JWT:Issuer"] ?? "https://localhost:5000";
    }
}
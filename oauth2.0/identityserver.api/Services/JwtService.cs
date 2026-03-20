using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using identityserver.api.Configuration;
using identityserver.api.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace identityserver.api.Services;

public sealed class JwtService : IJwtService
{
    private readonly JwtSettings _settings;

    public JwtService(IOptions<JwtSettings> options)
    {
        _settings = options.Value;
    }

    public string CreateAccessToken(User user, string? scope = null, IReadOnlyList<string>? groupNames = null, IReadOnlyList<string>? permissionCodes = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var scopeSet = ParseScopes(scope);

        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, user.Id.ToString()), new(ClaimTypes.NameIdentifier, user.Id.ToString()) };
        if (scopeSet.Contains("profile") || scopeSet.Count == 0)
        {
            claims.Add(new(JwtRegisteredClaimNames.UniqueName, user.UserName));
            claims.Add(new(ClaimTypes.Name, user.UserName));
        }
        if ((scopeSet.Contains("email") || scopeSet.Count == 0) && !string.IsNullOrEmpty(user.Email))
            claims.Add(new(JwtRegisteredClaimNames.Email, user.Email));
        if (!string.IsNullOrWhiteSpace(scope))
            claims.Add(new Claim("scope", scope.Trim()));
        AddGroupAndPermissionClaims(claims, groupNames, permissionCodes);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string CreateIdToken(User user, string audienceClientId, string? scope = null, IReadOnlyList<string>? groupNames = null, IReadOnlyList<string>? permissionCodes = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var scopeSet = ParseScopes(scope);

        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, user.Id.ToString()) };
        if (scopeSet.Contains("profile") || scopeSet.Count == 0)
        {
            claims.Add(new(JwtRegisteredClaimNames.UniqueName, user.UserName));
            claims.Add(new("preferred_username", user.UserName));
        }
        if ((scopeSet.Contains("email") || scopeSet.Count == 0) && !string.IsNullOrEmpty(user.Email))
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));
        AddGroupAndPermissionClaims(claims, groupNames, permissionCodes);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: audienceClientId,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static void AddGroupAndPermissionClaims(ICollection<Claim> claims, IReadOnlyList<string>? groupNames, IReadOnlyList<string>? permissionCodes)
    {
        if (groupNames is not null)
        {
            foreach (var name in groupNames.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.Ordinal))
                claims.Add(new Claim("group", name.Trim()));
        }
        if (permissionCodes is not null)
        {
            foreach (var code in permissionCodes.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct(StringComparer.Ordinal))
                claims.Add(new Claim("permission", code.Trim()));
        }
    }

    private static HashSet<string> ParseScopes(string? scope)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(scope)) return set;
        foreach (var s in scope.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            set.Add(s);
        return set;
    }
}

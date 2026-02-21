using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OAuthIdentityServer.Configuration;
using OAuthIdentityServer.Data;
using OAuthIdentityServer.Dto;
using OAuthIdentityServer.Models;
using System.IdentityModel.Tokens.Jwt;

namespace OAuthIdentityServer.Services;

public class TokenService : ITokenService
{
    private readonly IdentityDbContext _db;
    private readonly IClientService _clientService;
    private readonly OAuthOptions _options;
    private readonly SymmetricSecurityKey _signingKey;
    private const string CodeChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~";

    public TokenService(IdentityDbContext db, IClientService clientService, IOptions<OAuthOptions> options)
    {
        _db = db;
        _clientService = clientService;
        _options = options.Value;
        var keyBytes = Encoding.UTF8.GetBytes(_options.SigningKey.PadRight(32).Substring(0, 32));
        _signingKey = new SymmetricSecurityKey(keyBytes);
    }

    public byte[] GetSigningKeyBytes() => Encoding.UTF8.GetBytes(_options.SigningKey.PadRight(32).Substring(0, 32));

    public string GenerateAccessToken(User user, string clientId, IEnumerable<string> scopes)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Subject),
            new(JwtRegisteredClaimNames.Iss, _options.Issuer),
            new("client_id", clientId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        if (scopes.Contains("email")) claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));
        if (scopes.Contains("profile"))
        {
            if (!string.IsNullOrEmpty(user.Name)) claims.Add(new Claim(JwtRegisteredClaimNames.Name, user.Name));
            if (!string.IsNullOrEmpty(user.GivenName)) claims.Add(new Claim(JwtRegisteredClaimNames.GivenName, user.GivenName));
            if (!string.IsNullOrEmpty(user.FamilyName)) claims.Add(new Claim(JwtRegisteredClaimNames.FamilyName, user.FamilyName));
        }
        foreach (var scope in scopes)
            claims.Add(new Claim("scope", scope));

        var creds = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: clientId,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.AccessTokenLifetimeMinutes),
            signingCredentials: creds
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateIdToken(User user, string clientId, string? nonce)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Subject),
            new(JwtRegisteredClaimNames.Iss, _options.Issuer),
            new(JwtRegisteredClaimNames.Aud, clientId),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        if (!string.IsNullOrEmpty(user.Email)) claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));
        if (!string.IsNullOrEmpty(user.Name)) claims.Add(new Claim(JwtRegisteredClaimNames.Name, user.Name));
        if (!string.IsNullOrEmpty(user.GivenName)) claims.Add(new Claim(JwtRegisteredClaimNames.GivenName, user.GivenName));
        if (!string.IsNullOrEmpty(user.FamilyName)) claims.Add(new Claim(JwtRegisteredClaimNames.FamilyName, user.FamilyName));
        if (!string.IsNullOrEmpty(nonce)) claims.Add(new Claim("nonce", nonce));

        var creds = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: clientId,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.AccessTokenLifetimeMinutes),
            signingCredentials: creds
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateAuthorizationCode(string clientId, string userSubject, string redirectUri, string? scope, string? codeChallenge, string? codeChallengeMethod, string? nonce)
    {
        var code = GenerateRandomString(64);
        var entity = new AuthorizationCode
        {
            Code = code,
            ClientId = clientId,
            UserSubject = userSubject,
            RedirectUri = redirectUri,
            Scope = scope,
            CodeChallenge = codeChallenge,
            CodeChallengeMethod = codeChallengeMethod,
            Nonce = nonce,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_options.AuthorizationCodeLifetimeMinutes),
            IsUsed = false
        };
        _db.AuthorizationCodes.Add(entity);
        _db.SaveChanges();
        return code;
    }

    public async Task<TokenResponse?> ExchangeCodeForTokensAsync(string code, string redirectUri, string? codeVerifier, string clientId, string? clientSecret)
    {
        var authCode = await _db.AuthorizationCodes
            .FirstOrDefaultAsync(x => x.Code == code && !x.IsUsed && x.ExpiresAt > DateTime.UtcNow, CancellationToken.None);
        if (authCode == null || authCode.ClientId != clientId || authCode.RedirectUri != redirectUri)
            return null;

        var client = await _db.Clients.FirstOrDefaultAsync(c => c.ClientId == clientId, CancellationToken.None);
        if (client != null && client.RequirePkce && !_clientService.ValidateCodeChallenge(authCode.CodeChallenge, authCode.CodeChallengeMethod, codeVerifier))
            return null;

        authCode.IsUsed = true;
        _db.SaveChanges();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Subject == authCode.UserSubject, CancellationToken.None);
        if (user == null) return null;

        var scopes = (authCode.Scope ?? "openid").Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        var accessToken = GenerateAccessToken(user, clientId, scopes);
        string? refreshToken = null;
        if (client?.AllowOfflineAccess == true && scopes.Contains("offline_access"))
        {
            refreshToken = GenerateRandomString(64);
            _db.RefreshTokens.Add(new RefreshToken
            {
                Token = refreshToken,
                ClientId = clientId,
                UserSubject = user.Subject,
                Scope = authCode.Scope,
                ExpiresAt = DateTime.UtcNow.AddDays(_options.RefreshTokenLifetimeDays)
            });
            _db.SaveChanges();
        }

        var idToken = scopes.Contains("openid") ? GenerateIdToken(user, clientId, authCode.Nonce) : null;
        return new TokenResponse
        {
            AccessToken = accessToken,
            ExpiresIn = _options.AccessTokenLifetimeMinutes * 60,
            RefreshToken = refreshToken,
            Scope = authCode.Scope,
            IdToken = idToken
        };
    }

    public async Task<(string AccessToken, int ExpiresIn)?> RefreshAccessTokenAsync(string refreshToken, string clientId, string? clientSecret)
    {
        var rt = await _db.RefreshTokens.FirstOrDefaultAsync(
            x => x.Token == refreshToken && x.ClientId == clientId && !x.IsRevoked && x.ExpiresAt > DateTime.UtcNow,
            CancellationToken.None);
        if (rt == null) return null;

        rt.UsedAt = DateTime.UtcNow;
        rt.IsRevoked = true;
        _db.SaveChanges();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Subject == rt.UserSubject, CancellationToken.None);
        if (user == null) return null;

        var scopes = (rt.Scope ?? "openid").Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        var accessToken = GenerateAccessToken(user, clientId, scopes);
        return (accessToken, _options.AccessTokenLifetimeMinutes * 60);
    }

    public async Task<User?> GetUserFromAccessTokenAsync(string accessToken)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var validationParams = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _signingKey,
                ValidIssuer = _options.Issuer,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
            var principal = handler.ValidateToken(accessToken, validationParams, out _);
            var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (string.IsNullOrEmpty(sub)) return null;
            return await _db.Users.FirstOrDefaultAsync(u => u.Subject == sub, CancellationToken.None);
        }
        catch
        {
            return null;
        }
    }

    private static string GenerateRandomString(int length)
    {
        var bytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        var sb = new StringBuilder(length);
        for (var i = 0; i < length; i++)
            sb.Append(CodeChars[bytes[i] % CodeChars.Length]);
        return sb.ToString();
    }
}

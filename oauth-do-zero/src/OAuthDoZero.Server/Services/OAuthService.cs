using Microsoft.EntityFrameworkCore;
using OAuthDoZero.Server.Data;
using OAuthDoZero.Server.DTOs;
using OAuthDoZero.Server.Models;

namespace OAuthDoZero.Server.Services;

/// <summary>
/// Serviço principal para operações OAuth 2.0
/// </summary>
public interface IOAuthService
{
    Task<AuthorizationCode> CreateAuthorizationCodeAsync(User user, Client client, AuthorizationRequest request);
    Task<AuthorizationCode?> GetAuthorizationCodeAsync(string code);
    Task<AccessToken> CreateAccessTokenAsync(User user, Client client, IEnumerable<string> scopes);
    Task<RefreshToken> CreateRefreshTokenAsync(AccessToken accessToken);
    Task<Client?> ValidateClientAsync(string clientId, string? clientSecret = null);
    Task<bool> ValidateRedirectUriAsync(string clientId, string redirectUri);
    Task<User?> GetUserByUsernameAsync(string username);
    Task<User?> GetUserByIdAsync(string userId);
    Task<bool> ValidateUserPasswordAsync(string username, string password);
    Task RevokeTokenAsync(string tokenHash);
}

public class OAuthService : IOAuthService
{
    private readonly OAuthDbContext _context;
    private readonly ICryptographyService _cryptoService;
    private readonly IJwtService _jwtService;

    public OAuthService(OAuthDbContext context, ICryptographyService cryptoService, IJwtService jwtService)
    {
        _context = context;
        _cryptoService = cryptoService;
        _jwtService = jwtService;
    }

    public async Task<AuthorizationCode> CreateAuthorizationCodeAsync(User user, Client client, AuthorizationRequest request)
    {
        var code = new AuthorizationCode
        {
            Code = _cryptoService.GenerateSecureRandomString(32),
            UserId = user.Id,
            ClientId = client.ClientId,
            RedirectUri = request.RedirectUri!,
            Scopes = request.Scope ?? "openid",
            State = request.State,
            CodeChallenge = request.CodeChallenge,
            CodeChallengeMethod = request.CodeChallengeMethod ?? "S256",
            Nonce = request.Nonce,
            ExpiresAt = DateTime.UtcNow.AddMinutes(client.AuthorizationCodeLifetime / 60),
            CreatedAt = DateTime.UtcNow
        };

        _context.AuthorizationCodes.Add(code);
        await _context.SaveChangesAsync();

        return code;
    }

    public async Task<AuthorizationCode?> GetAuthorizationCodeAsync(string code)
    {
        return await _context.AuthorizationCodes
            .Include(ac => ac.User)
            .Include(ac => ac.Client)
            .FirstOrDefaultAsync(ac => ac.Code == code && 
                                     !ac.IsUsed && 
                                     ac.ExpiresAt > DateTime.UtcNow);
    }

    public async Task<AccessToken> CreateAccessTokenAsync(User user, Client client, IEnumerable<string> scopes)
    {
        var jwtId = _jwtService.GenerateJti();
        var expiry = TimeSpan.FromSeconds(client.AccessTokenLifetime);
        var jwtToken = _jwtService.GenerateAccessToken(user, client, scopes, expiry);
        var tokenHash = _cryptoService.ComputeSha256Hash(jwtToken);

        var accessToken = new AccessToken
        {
            TokenId = jwtId,
            TokenHash = tokenHash,
            UserId = user.Id,
            ClientId = client.ClientId,
            Scopes = string.Join(" ", scopes),
            JwtId = jwtId,
            ExpiresAt = DateTime.UtcNow.Add(expiry),
            CreatedAt = DateTime.UtcNow
        };

        _context.AccessTokens.Add(accessToken);
        await _context.SaveChangesAsync();

        return accessToken;
    }

    public async Task<RefreshToken> CreateRefreshTokenAsync(AccessToken accessToken)
    {
        var client = await _context.Clients.FindAsync(accessToken.ClientId);
        if (client == null || !client.AllowRefreshToken)
            throw new InvalidOperationException("Refresh tokens not allowed for this client");

        var tokenValue = _cryptoService.GenerateSecureRandomString(64);
        var tokenHash = _cryptoService.ComputeSha256Hash(tokenValue);

        var refreshToken = new RefreshToken
        {
            TokenId = Guid.NewGuid().ToString(),
            TokenHash = tokenHash,
            UserId = accessToken.UserId,
            ClientId = accessToken.ClientId,
            AccessTokenId = accessToken.TokenId,
            Scopes = accessToken.Scopes,
            ExpiresAt = DateTime.UtcNow.AddSeconds(client.RefreshTokenLifetime),
            CreatedAt = DateTime.UtcNow
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return refreshToken;
    }

    public async Task<Client?> ValidateClientAsync(string clientId, string? clientSecret = null)
    {
        var client = await _context.Clients
            .FirstOrDefaultAsync(c => c.ClientId == clientId && c.IsActive);

        if (client == null) return null;

        // Para clientes confidenciais, verificar secret
        if (client.ClientType == "confidential" && !string.IsNullOrEmpty(clientSecret))
        {
            if (!BCrypt.Net.BCrypt.Verify(clientSecret, client.ClientSecret))
                return null;
        }

        return client;
    }

    public async Task<bool> ValidateRedirectUriAsync(string clientId, string redirectUri)
    {
        var client = await _context.Clients.FindAsync(clientId);
        if (client == null) return false;

        var allowedUris = client.RedirectUris.Split(',', StringSplitOptions.RemoveEmptyEntries);
        return allowedUris.Contains(redirectUri);
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
    }

    public async Task<User?> GetUserByIdAsync(string userId)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
    }

    public async Task<bool> ValidateUserPasswordAsync(string username, string password)
    {
        var user = await GetUserByUsernameAsync(username);
        if (user == null) return false;

        return _cryptoService.VerifyPassword(password, user.Salt, user.PasswordHash);
    }

    public async Task RevokeTokenAsync(string tokenHash)
    {
        var accessToken = await _context.AccessTokens
            .FirstOrDefaultAsync(at => at.TokenHash == tokenHash);

        if (accessToken != null)
        {
            accessToken.IsRevoked = true;
            accessToken.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
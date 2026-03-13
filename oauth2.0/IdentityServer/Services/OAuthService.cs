using IdentityServer.Configuration;
using IdentityServer.Data;
using IdentityServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace IdentityServer.Services;

public sealed class OAuthService : IOAuthService
{
    private readonly AuthDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly JwtSettings _settings;

    public OAuthService(AuthDbContext db, IPasswordHasher passwordHasher, IOptions<JwtSettings> options)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _settings = options.Value;
    }

    public async Task<Client?> GetClientByIdAsync(string clientId, CancellationToken cancellationToken = default)
    {
        return await _db.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ClientId == clientId.Trim(), cancellationToken);
    }

    public async Task<Client?> ValidateClientAsync(string clientId, string clientSecret, CancellationToken cancellationToken = default)
    {
        var client = await _db.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ClientId == clientId.Trim(), cancellationToken);

        if (client is null || string.IsNullOrWhiteSpace(clientSecret))
            return null;

        var valid = _passwordHasher.VerifyPassword(clientSecret, client.ClientSecretHash, client.ClientSecretSalt);
        return valid ? client : null;
    }

    public bool IsRedirectUriAllowed(Client client, string redirectUri)
    {
        if (string.IsNullOrWhiteSpace(redirectUri)) return false;
        var allowed = client.RedirectUris.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return allowed.Any(u => string.Equals(u, redirectUri.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public async Task<AuthorizationCode> CreateAuthorizationCodeAsync(User user, Client client, string redirectUri, string? scope, string? state, CancellationToken cancellationToken = default)
    {
        var code = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var expiresAt = DateTime.UtcNow.AddMinutes(_settings.AuthorizationCodeExpirationMinutes);

        var authCode = new AuthorizationCode
        {
            UserId = user.Id,
            ClientId = client.Id,
            Code = code,
            RedirectUri = redirectUri,
            Scope = scope,
            State = state,
            ExpiresAtUtc = expiresAt
        };

        _db.AuthorizationCodes.Add(authCode);
        await _db.SaveChangesAsync(cancellationToken);
        return authCode;
    }

    public async Task<(User User, Client Client)?> ConsumeAuthorizationCodeAsync(string code, string redirectUri, string clientId, CancellationToken cancellationToken = default)
    {
        var codeTrimmed = code?.Trim();
        if (string.IsNullOrEmpty(codeTrimmed))
            return null;

        var entity = await _db.AuthorizationCodes
            .Include(a => a.User)
            .Include(a => a.Client)
            .FirstOrDefaultAsync(a => a.Code == codeTrimmed, cancellationToken);

        if (entity is null)
            return null;

        if (entity.IsUsed)
            return null; // código já utilizado (uso único)

        if (entity.IsExpired)
            return null; // código expirado (validade ~10 min)

        if (!string.Equals(entity.Client.ClientId, clientId.Trim(), StringComparison.Ordinal))
            return null;

        if (!RedirectUriMatches(entity.RedirectUri, redirectUri))
            return null; // redirect_uri deve ser idêntico ao usado no /authorize

        entity.UsedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return (entity.User, entity.Client);
    }

    /// <summary>Compara redirect_uri normalizado (trim, barra final ignorada).</summary>
    private static bool RedirectUriMatches(string stored, string requested)
    {
        var s = stored?.Trim().TrimEnd('/') ?? "";
        var r = requested?.Trim().TrimEnd('/') ?? "";
        return string.Equals(s, r, StringComparison.OrdinalIgnoreCase);
    }
}

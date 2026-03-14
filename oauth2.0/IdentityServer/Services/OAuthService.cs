using System.Security.Cryptography;
using System.Text;
using IdentityServer.Configuration;
using IdentityServer.Data;
using IdentityServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

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

    public async Task<AuthorizationCode> CreateAuthorizationCodeAsync(User user, Client client, string redirectUri, string? scope, string? state, string? codeChallenge, string? codeChallengeMethod, CancellationToken cancellationToken = default)
    {
        var code = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var expiresAt = DateTime.UtcNow.AddMinutes(_settings.AuthorizationCodeExpirationMinutes);

        var method = NormalizeChallengeMethod(codeChallengeMethod);
        if (!string.IsNullOrEmpty(codeChallenge) && method is null)
            method = "S256"; // RFC: default quando só code_challenge é enviado

        var authCode = new AuthorizationCode
        {
            UserId = user.Id,
            ClientId = client.Id,
            Code = code,
            RedirectUri = redirectUri,
            Scope = scope,
            State = state,
            CodeChallenge = string.IsNullOrWhiteSpace(codeChallenge) ? null : codeChallenge.Trim(),
            CodeChallengeMethod = method,
            ExpiresAtUtc = expiresAt
        };

        _db.AuthorizationCodes.Add(authCode);
        await _db.SaveChangesAsync(cancellationToken);
        return authCode;
    }

    private static string? NormalizeChallengeMethod(string? value)
    {
        var v = value?.Trim();
        if (string.IsNullOrEmpty(v)) return null;
        if (string.Equals(v, "S256", StringComparison.OrdinalIgnoreCase)) return "S256";
        if (string.Equals(v, "plain", StringComparison.OrdinalIgnoreCase)) return "plain";
        return null;
    }

    public async Task<(User User, Client Client)?> ConsumeAuthorizationCodeAsync(string code, string redirectUri, string clientId, string? codeVerifier, CancellationToken cancellationToken = default)
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
            return null;

        if (entity.IsExpired)
            return null;

        if (!string.Equals(entity.Client.ClientId, clientId.Trim(), StringComparison.Ordinal))
            return null;

        if (!RedirectUriMatches(entity.RedirectUri, redirectUri))
            return null;

        if (!string.IsNullOrEmpty(entity.CodeChallenge))
        {
            if (string.IsNullOrWhiteSpace(codeVerifier))
                return null;
            var verifier = codeVerifier.Trim();
            if (verifier.Length < 43 || verifier.Length > 128)
                return null;
            var expectedChallenge = entity.CodeChallengeMethod == "plain"
                ? verifier
                : ComputeS256Challenge(verifier);
            if (!string.Equals(entity.CodeChallenge, expectedChallenge, StringComparison.Ordinal))
                return null;
        }

        entity.UsedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return (entity.User, entity.Client);
    }

    /// <summary>RFC 7636: Base64URL(SHA256(code_verifier)).</summary>
    private static string ComputeS256Challenge(string codeVerifier)
    {
        var bytes = Encoding.ASCII.GetBytes(codeVerifier);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    /// <summary>Compara redirect_uri normalizado (trim, barra final ignorada).</summary>
    private static bool RedirectUriMatches(string stored, string requested)
    {
        var s = stored?.Trim().TrimEnd('/') ?? "";
        var r = requested?.Trim().TrimEnd('/') ?? "";
        return string.Equals(s, r, StringComparison.OrdinalIgnoreCase);
    }
}

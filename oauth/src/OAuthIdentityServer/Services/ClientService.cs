using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using OAuthIdentityServer.Data;
using OAuthIdentityServer.Models;

namespace OAuthIdentityServer.Services;

public class ClientService : IClientService
{
    private readonly IdentityDbContext _db;

    public ClientService(IdentityDbContext db) => _db = db;

    public Task<Client?> GetByClientIdAsync(string clientId, CancellationToken ct = default)
        => _db.Clients.FirstOrDefaultAsync(c => c.ClientId == clientId && c.IsEnabled, ct);

    public bool ValidateRedirectUri(Client client, string redirectUri)
    {
        var allowed = client.RedirectUris?.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(u => u.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>();
        return allowed.Contains(redirectUri);
    }

    public bool ValidateClientSecret(Client client, string? clientSecret)
    {
        if (!client.RequireClientSecret) return true;
        if (string.IsNullOrEmpty(clientSecret)) return false;
        return !string.IsNullOrEmpty(client.ClientSecretHash) && BCrypt.Net.BCrypt.Verify(clientSecret, client.ClientSecretHash);
    }

    public bool IsValidGrantType(Client client, string grantType)
    {
        var allowed = client.AllowedGrantTypes?.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(g => g.Trim()).ToHashSet(StringComparer.Ordinal) ?? new HashSet<string>();
        return allowed.Contains(grantType);
    }

    public bool IsValidScope(Client client, string? scope)
    {
        if (string.IsNullOrWhiteSpace(scope)) return true;
        var allowed = client.AllowedScopes?.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim()).ToHashSet(StringComparer.Ordinal) ?? new HashSet<string>();
        return scope.Split(' ', StringSplitOptions.RemoveEmptyEntries).All(s => allowed.Contains(s));
    }

    public bool ValidateCodeChallenge(string? codeChallenge, string? codeChallengeMethod, string? codeVerifier)
    {
        if (string.IsNullOrEmpty(codeChallenge)) return true; // PKCE opcional se cliente não enviou
        if (string.IsNullOrEmpty(codeVerifier)) return false;

        if (string.Equals(codeChallengeMethod, "S256", StringComparison.OrdinalIgnoreCase))
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
            var computed = Base64UrlEncode(hash);
            return string.Equals(computed, codeChallenge, StringComparison.Ordinal);
        }
        if (string.Equals(codeChallengeMethod, "plain", StringComparison.OrdinalIgnoreCase))
            return codeVerifier == codeChallenge;
        return false;
    }

    private static string Base64UrlEncode(byte[] input)
    {
        var base64 = Convert.ToBase64String(input);
        return base64.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}

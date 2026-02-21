using OAuthIdentityServer.Models;

namespace OAuthIdentityServer.Services;

public interface IClientService
{
    Task<Client?> GetByClientIdAsync(string clientId, CancellationToken ct = default);
    bool ValidateRedirectUri(Client client, string redirectUri);
    bool ValidateClientSecret(Client client, string? clientSecret);
    bool IsValidGrantType(Client client, string grantType);
    bool IsValidScope(Client client, string? scope);
    bool ValidateCodeChallenge(string? codeChallenge, string? codeChallengeMethod, string? codeVerifier);
}

using identityserver.api.Models;

namespace identityserver.api.Services;

public interface IOAuthService
{
    Task<Client?> GetClientByIdAsync(string clientId, CancellationToken cancellationToken = default);
    Task<Client?> ValidateClientAsync(string clientId, string clientSecret, CancellationToken cancellationToken = default);
    bool IsRedirectUriAllowed(Client client, string redirectUri);
    Task<AuthorizationCode> CreateAuthorizationCodeAsync(User user, Client client, string redirectUri, string? scope, string? state, string? codeChallenge, string? codeChallengeMethod, CancellationToken cancellationToken = default);
    Task<(User User, Client Client, string? Scope)?> ConsumeAuthorizationCodeAsync(string code, string redirectUri, string clientId, string? codeVerifier, CancellationToken cancellationToken = default);
}

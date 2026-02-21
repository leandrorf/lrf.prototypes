using OAuthIdentityServer.Dto;
using OAuthIdentityServer.Models;

namespace OAuthIdentityServer.Services;

public interface ITokenService
{
    string GenerateAccessToken(User user, string clientId, IEnumerable<string> scopes);
    string GenerateIdToken(User user, string clientId, string? nonce);
    string GenerateAuthorizationCode(string clientId, string userSubject, string redirectUri, string? scope, string? codeChallenge, string? codeChallengeMethod, string? nonce);
    Task<TokenResponse?> ExchangeCodeForTokensAsync(string code, string redirectUri, string? codeVerifier, string clientId, string? clientSecret);
    Task<(string AccessToken, int ExpiresIn)?> RefreshAccessTokenAsync(string refreshToken, string clientId, string? clientSecret);
    Task<User?> GetUserFromAccessTokenAsync(string accessToken);
    byte[] GetSigningKeyBytes();
}

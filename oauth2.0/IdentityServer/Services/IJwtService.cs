using IdentityServer.Models;

namespace IdentityServer.Services;

public interface IJwtService
{
    string CreateAccessToken(User user, string? scope = null);
    /// <summary>OpenID Connect: JWT de identidade (audience = client_id). Claims conforme scope: openid (sub), profile (name), email.</summary>
    string CreateIdToken(User user, string audienceClientId, string? scope = null);
}

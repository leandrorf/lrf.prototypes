using IdentityServer.Models;

namespace IdentityServer.Services;

public interface IJwtService
{
    string CreateAccessToken(User user);
    /// <summary>OpenID Connect: JWT de identidade (audience = client_id).</summary>
    string CreateIdToken(User user, string audienceClientId);
}

using IdentityServer.Models;

namespace IdentityServer.Services;

public interface IJwtService
{
    string CreateAccessToken(User user);
}

using lrfdev.auth.api.Models;

namespace lrfdev.auth.api.Services;

public interface ITokenService
{
    string GenerateAccessToken(User user, IReadOnlyCollection<string> permissions);
    string GenerateClientAccessToken(string clientId, IReadOnlyCollection<string> scopes);
}

using IdentityServer.Models;

namespace IdentityServer.Services;

public interface IRefreshTokenService
{
    Task<RefreshToken> CreateAsync(User user, CancellationToken cancellationToken = default);
    Task<RefreshToken?> FindValidAsync(string token, CancellationToken cancellationToken = default);
    Task RevokeAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
}

using OAuthIdentityServer.Models;

namespace OAuthIdentityServer.Services;

public interface IUserService
{
    Task<User?> FindBySubjectAsync(string subject, CancellationToken ct = default);
    Task<User?> FindByUserNameAsync(string userName, CancellationToken ct = default);
    Task<bool> ValidateCredentialsAsync(string userName, string password, CancellationToken ct = default);
    Task<User?> GetBySubjectAsync(string subject, CancellationToken ct = default);
}

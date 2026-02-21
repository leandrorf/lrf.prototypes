using Microsoft.EntityFrameworkCore;
using OAuthIdentityServer.Data;
using OAuthIdentityServer.Models;

namespace OAuthIdentityServer.Services;

public class UserService : IUserService
{
    private readonly IdentityDbContext _db;

    public UserService(IdentityDbContext db) => _db = db;

    public Task<User?> FindBySubjectAsync(string subject, CancellationToken ct = default)
        => _db.Users.FirstOrDefaultAsync(u => u.Subject == subject && u.IsActive, ct);

    public Task<User?> FindByUserNameAsync(string userName, CancellationToken ct = default)
        => _db.Users.FirstOrDefaultAsync(u => u.NormalizedUserName == userName.ToUpperInvariant() && u.IsActive, ct);

    public async Task<bool> ValidateCredentialsAsync(string userName, string password, CancellationToken ct = default)
    {
        var user = await FindByUserNameAsync(userName, ct);
        return user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
    }

    public Task<User?> GetBySubjectAsync(string subject, CancellationToken ct = default)
        => FindBySubjectAsync(subject, ct);
}

using System.Security.Cryptography;
using identityserver.api.Configuration;
using identityserver.api.Data;
using identityserver.api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace identityserver.api.Services;

public sealed class RefreshTokenService : IRefreshTokenService
{
    private readonly AuthDbContext _db;
    private readonly JwtSettings _settings;

    public RefreshTokenService(AuthDbContext db, IOptions<JwtSettings> options)
    {
        _db = db;
        _settings = options.Value;
    }

    public async Task<RefreshToken> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var expiresAt = DateTime.UtcNow.AddDays(_settings.RefreshTokenExpirationDays);

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = token,
            ExpiresAtUtc = expiresAt
        };

        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync(cancellationToken);

        return refreshToken;
    }

    public async Task<RefreshToken?> FindValidAsync(string token, CancellationToken cancellationToken = default)
    {
        var entity = await _db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);

        return entity is not null && entity.IsActive ? entity : null;
    }

    public async Task RevokeAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        refreshToken.RevokedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }
}

using identityserver.api.Data;
using Microsoft.EntityFrameworkCore;

namespace identityserver.api.Services;

public sealed class PermissionService : IPermissionService
{
    private readonly AuthDbContext _db;

    public PermissionService(AuthDbContext db) => _db = db;

    public async Task<IReadOnlyList<string>> GetGroupNamesForUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _db.UserGroupMemberships
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .Join(_db.PermissionGroups, m => m.GroupId, g => g.Id, (m, g) => g.Name)
            .OrderBy(n => n)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetFeatureCodesForUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var groupIds = await _db.UserGroupMemberships.AsNoTracking()
            .Where(m => m.UserId == userId)
            .Select(m => m.GroupId)
            .ToListAsync(cancellationToken);
        if (groupIds.Count == 0)
            return Array.Empty<string>();

        return await _db.GroupFeatureGrants.AsNoTracking()
            .Where(gf => groupIds.Contains(gf.GroupId))
            .Join(_db.AppFeatures, gf => gf.FeatureId, f => f.Id, (gf, f) => f.Code)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> CanUserAccessDeviceAsync(int userId, string deviceExternalId, CancellationToken cancellationToken = default)
    {
        var normalizedId = deviceExternalId.Trim();
        var device = await _db.RegisteredDevices.AsNoTracking()
            .FirstOrDefaultAsync(d => d.ExternalId == normalizedId, cancellationToken);
        if (device is null)
            return false;

        var userGroupIds = await _db.UserGroupMemberships.AsNoTracking()
            .Where(m => m.UserId == userId)
            .Select(m => m.GroupId)
            .ToListAsync(cancellationToken);
        if (userGroupIds.Count == 0)
            return false;

        return await _db.GroupTvAccesses.AsNoTracking()
            .AnyAsync(a => userGroupIds.Contains(a.GroupId) && a.DeviceGroup == device.DeviceGroup, cancellationToken);
    }
}

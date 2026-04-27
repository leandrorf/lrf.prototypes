using Microsoft.EntityFrameworkCore;
using lrfdev.auth.api.Data;

namespace lrfdev.auth.api.Services;

public sealed class PermissionResolver(AuthDbContext context) : IPermissionResolver
{
    public async Task<IReadOnlyCollection<string>> ResolveAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var permissionsByUser = await context.UserPermissions
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.Permission.Name)
            .ToListAsync(cancellationToken);

        var permissionsByGroup = await context.UserGroups
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .SelectMany(x => x.Group.GroupPermissions.Select(gp => gp.Permission.Name))
            .ToListAsync(cancellationToken);

        return permissionsByUser
            .Concat(permissionsByGroup)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}

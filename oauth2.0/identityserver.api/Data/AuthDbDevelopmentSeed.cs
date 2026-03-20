using identityserver.api.Models;
using Microsoft.EntityFrameworkCore;

namespace identityserver.api.Data;

/// <summary>Seed de RBAC, TV e associações para desenvolvimento (executado na inicialização em Program.cs).</summary>
public static class AuthDbDevelopmentSeed
{
    public static async Task SeedRbacAndDevicesAsync(AuthDbContext db, CancellationToken cancellationToken = default)
    {
        const string grpAdmin = "Administrators";
        const string grpOperator = "Operators";
        const string featRead = "app.demo.read";
        const string featWrite = "app.demo.write";
        const string tvGroup = "TV-Recepcao";
        const string devTv = "tv-001";

        async Task<PermissionGroup> EnsureGroupAsync(string name, string? description = null)
        {
            var g = await db.PermissionGroups.FirstOrDefaultAsync(x => x.Name == name, cancellationToken);
            if (g is null)
            {
                g = new PermissionGroup { Name = name, Description = description };
                db.PermissionGroups.Add(g);
                await db.SaveChangesAsync(cancellationToken);
            }
            return g;
        }

        async Task<AppFeature> EnsureFeatureAsync(string code, string? displayName = null)
        {
            var f = await db.AppFeatures.FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
            if (f is null)
            {
                f = new AppFeature { Code = code, DisplayName = displayName };
                db.AppFeatures.Add(f);
                await db.SaveChangesAsync(cancellationToken);
            }
            return f;
        }

        var adminG = await EnsureGroupAsync(grpAdmin, "Administradores (todas as permissões demo e TV).");
        var opG = await EnsureGroupAsync(grpOperator, "Operadores (leitura demo + TV recepção).");

        var readF = await EnsureFeatureAsync(featRead, "Demo: leitura");
        var writeF = await EnsureFeatureAsync(featWrite, "Demo: escrita");

        async Task EnsureGrantAsync(int groupId, int featureId)
        {
            var exists = await db.GroupFeatureGrants.AnyAsync(g => g.GroupId == groupId && g.FeatureId == featureId, cancellationToken);
            if (!exists)
            {
                db.GroupFeatureGrants.Add(new GroupFeatureGrant { GroupId = groupId, FeatureId = featureId });
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        await EnsureGrantAsync(adminG.Id, readF.Id);
        await EnsureGrantAsync(adminG.Id, writeF.Id);
        await EnsureGrantAsync(opG.Id, readF.Id);

        if (!await db.RegisteredDevices.AnyAsync(d => d.ExternalId == devTv, cancellationToken))
        {
            db.RegisteredDevices.Add(new RegisteredDevice
            {
                ExternalId = devTv,
                DeviceGroup = tvGroup,
                DisplayName = "TV Recepção (dev)"
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        async Task EnsureTvAccessAsync(int groupId, string deviceGroup)
        {
            var exists = await db.GroupTvAccesses.AnyAsync(a => a.GroupId == groupId && a.DeviceGroup == deviceGroup, cancellationToken);
            if (!exists)
            {
                db.GroupTvAccesses.Add(new GroupTvAccess { GroupId = groupId, DeviceGroup = deviceGroup });
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        await EnsureTvAccessAsync(adminG.Id, tvGroup);
        await EnsureTvAccessAsync(opG.Id, tvGroup);

        var userIdsNoGroup = await db.Users
            .Where(u => !db.UserGroupMemberships.Any(m => m.UserId == u.Id))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        foreach (var uid in userIdsNoGroup)
            db.UserGroupMemberships.Add(new UserGroupMembership { UserId = uid, GroupId = opG.Id });

        if (userIdsNoGroup.Count > 0)
            await db.SaveChangesAsync(cancellationToken);
    }
}

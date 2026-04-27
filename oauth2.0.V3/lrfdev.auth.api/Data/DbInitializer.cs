using Microsoft.EntityFrameworkCore;
using lrfdev.auth.api.Models;
using lrfdev.auth.api.Security;

namespace lrfdev.auth.api.Data;

public sealed class DbInitializer(AuthDbContext context, IPasswordHasher passwordHasher)
{
    private static string MergeSpaceSeparatedScopes(string current, string ensurePresent)
    {
        var set = new HashSet<string>(
            current.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            StringComparer.Ordinal);
        foreach (var part in ensurePresent.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            set.Add(part);
        }

        return string.Join(' ', set.OrderBy(x => x, StringComparer.Ordinal));
    }

    public async Task InitializeAsync()
    {
        await context.Database.EnsureCreatedAsync();

        if (!await context.Users.AnyAsync())
        {
            var adminPermission = new Permission
            {
                Id = Guid.NewGuid(),
                Name = "auth.manage",
                Description = "Manage identity server settings"
            };

            var readPermission = new Permission
            {
                Id = Guid.NewGuid(),
                Name = "auth.read",
                Description = "Read identity resources"
            };

            var adminGroup = new PermissionGroup
            {
                Id = Guid.NewGuid(),
                Name = "admins"
            };

            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                Username = "admin",
                Email = "admin@lrfdev.local",
                PasswordHash = passwordHasher.HashPassword("Admin@123"),
                IsActive = true
            };

            context.Permissions.AddRange(adminPermission, readPermission);
            context.Groups.Add(adminGroup);
            context.Users.Add(adminUser);

            context.GroupPermissions.AddRange(
                new GroupPermission
                {
                    GroupId = adminGroup.Id,
                    PermissionId = adminPermission.Id
                },
                new GroupPermission
                {
                    GroupId = adminGroup.Id,
                    PermissionId = readPermission.Id
                });

            context.UserGroups.Add(new UserGroup
            {
                UserId = adminUser.Id,
                GroupId = adminGroup.Id
            });
        }
        
        var hasPkceClient = await context.OAuthClients.AnyAsync(x => x.ClientId == "lrfdev.web.pkce");
        if (!hasPkceClient)
        {
            context.OAuthClients.Add(new OAuthClient
            {
                Id = Guid.NewGuid(),
                ClientId = "lrfdev.web.pkce",
                Name = "lrfdev auth web (pkce)",
                RedirectUris = "https://localhost:7040/signin-oidc-dev",
                AllowedScopes = "openid profile auth.read auth.manage",
                RequirePkce = true,
                IsConfidentialClient = false,
                AllowDeviceAuthorization = false,
                IsActive = true
            });
        }

        const string m2mClientId = "lrfdev.service.m2m";
        const string m2mScopes = "auth.read auth.manage orders.read orders.write";

        var hasM2mClient = await context.OAuthClients.AnyAsync(x => x.ClientId == m2mClientId);
        if (!hasM2mClient)
        {
            context.OAuthClients.Add(new OAuthClient
            {
                Id = Guid.NewGuid(),
                ClientId = m2mClientId,
                ClientSecretHash = passwordHasher.HashPassword("m2m-secret-123"),
                Name = "lrfdev service to service",
                RedirectUris = string.Empty,
                AllowedScopes = m2mScopes,
                RequirePkce = false,
                IsConfidentialClient = true,
                AllowDeviceAuthorization = false,
                IsActive = true
            });
        }
        else
        {
            var m2m = await context.OAuthClients.FirstAsync(x => x.ClientId == m2mClientId);
            m2m.AllowedScopes = MergeSpaceSeparatedScopes(m2m.AllowedScopes, m2mScopes);
        }

        const string tvClientId = "lrfdev.tv.device";
        if (!await context.OAuthClients.AnyAsync(x => x.ClientId == tvClientId))
        {
            context.OAuthClients.Add(new OAuthClient
            {
                Id = Guid.NewGuid(),
                ClientId = tvClientId,
                Name = "lrfdev TV / IoT (device code)",
                RedirectUris = string.Empty,
                AllowedScopes = "openid profile auth.read auth.manage",
                RequirePkce = false,
                IsConfidentialClient = false,
                AllowDeviceAuthorization = true,
                IsActive = true
            });
        }
        else
        {
            var tv = await context.OAuthClients.FirstAsync(x => x.ClientId == tvClientId);
            tv.AllowDeviceAuthorization = true;
        }

        await context.SaveChangesAsync();
    }
}

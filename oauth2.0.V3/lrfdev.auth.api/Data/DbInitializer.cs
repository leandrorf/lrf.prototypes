using Microsoft.EntityFrameworkCore;
using lrfdev.auth.api.Models;
using lrfdev.auth.api.Security;

namespace lrfdev.auth.api.Data;

public sealed class DbInitializer(AuthDbContext context, IPasswordHasher passwordHasher)
{
    public async Task InitializeAsync()
    {
        await context.Database.EnsureCreatedAsync();

        if (await context.Users.AnyAsync())
        {
            return;
        }

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

        await context.SaveChangesAsync();
    }
}

using identityserver.api.Models;
using Microsoft.EntityFrameworkCore;

namespace identityserver.api.Data;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<AuthorizationCode> AuthorizationCodes => Set<AuthorizationCode>();

    public DbSet<PermissionGroup> PermissionGroups => Set<PermissionGroup>();
    public DbSet<AppFeature> AppFeatures => Set<AppFeature>();
    public DbSet<UserGroupMembership> UserGroupMemberships => Set<UserGroupMembership>();
    public DbSet<GroupFeatureGrant> GroupFeatureGrants => Set<GroupFeatureGrant>();
    public DbSet<RegisteredDevice> RegisteredDevices => Set<RegisteredDevice>();
    public DbSet<GroupTvAccess> GroupTvAccesses => Set<GroupTvAccess>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var user = modelBuilder.Entity<User>();
        user.HasIndex(u => u.UserName).IsUnique();
        user.Property(u => u.UserName).IsRequired().HasMaxLength(100);
        user.Property(u => u.PasswordHash).IsRequired().HasMaxLength(256);
        user.Property(u => u.PasswordSalt).IsRequired().HasMaxLength(128);
        user.Property(u => u.Email).HasMaxLength(256);

        var refreshToken = modelBuilder.Entity<RefreshToken>();
        refreshToken.HasIndex(rt => rt.Token).IsUnique();
        refreshToken.HasOne(rt => rt.User).WithMany().HasForeignKey(rt => rt.UserId).OnDelete(DeleteBehavior.Cascade);

        var client = modelBuilder.Entity<Client>();
        client.HasIndex(c => c.ClientId).IsUnique();
        client.Property(c => c.ClientId).IsRequired().HasMaxLength(200);
        client.Property(c => c.RedirectUris).IsRequired().HasMaxLength(2000);
        client.Property(c => c.AllowedGrantTypes).IsRequired().HasMaxLength(200);

        var authCode = modelBuilder.Entity<AuthorizationCode>();
        authCode.HasIndex(a => a.Code).IsUnique();
        authCode.HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
        authCode.HasOne(a => a.Client).WithMany().HasForeignKey(a => a.ClientId).OnDelete(DeleteBehavior.Cascade);

        var permissionGroup = modelBuilder.Entity<PermissionGroup>();
        permissionGroup.ToTable("permission_groups");
        permissionGroup.HasIndex(g => g.Name).IsUnique();
        permissionGroup.Property(g => g.Name).IsRequired().HasMaxLength(100);

        var appFeature = modelBuilder.Entity<AppFeature>();
        appFeature.ToTable("app_features");
        appFeature.HasIndex(f => f.Code).IsUnique();
        appFeature.Property(f => f.Code).IsRequired().HasMaxLength(120);

        var userGroupMembership = modelBuilder.Entity<UserGroupMembership>();
        userGroupMembership.ToTable("user_group_memberships");
        userGroupMembership.HasKey(m => new { m.UserId, m.GroupId });
        userGroupMembership.HasOne(m => m.User).WithMany().HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);
        userGroupMembership.HasOne(m => m.Group).WithMany(g => g.UserMemberships).HasForeignKey(m => m.GroupId).OnDelete(DeleteBehavior.Cascade);

        var groupFeatureGrant = modelBuilder.Entity<GroupFeatureGrant>();
        groupFeatureGrant.ToTable("group_feature_grants");
        groupFeatureGrant.HasKey(g => new { g.GroupId, g.FeatureId });
        groupFeatureGrant.HasOne(g => g.Group).WithMany(pg => pg.FeatureGrants).HasForeignKey(g => g.GroupId).OnDelete(DeleteBehavior.Cascade);
        groupFeatureGrant.HasOne(g => g.Feature).WithMany(f => f.GroupGrants).HasForeignKey(g => g.FeatureId).OnDelete(DeleteBehavior.Cascade);

        var registeredDevice = modelBuilder.Entity<RegisteredDevice>();
        registeredDevice.ToTable("registered_devices");
        registeredDevice.HasIndex(d => d.ExternalId).IsUnique();

        var groupTvAccess = modelBuilder.Entity<GroupTvAccess>();
        groupTvAccess.ToTable("group_tv_access");
        groupTvAccess.HasIndex(a => new { a.GroupId, a.DeviceGroup }).IsUnique();
        groupTvAccess.HasOne(a => a.Group).WithMany(g => g.TvAccessRules).HasForeignKey(a => a.GroupId).OnDelete(DeleteBehavior.Cascade);
    }
}


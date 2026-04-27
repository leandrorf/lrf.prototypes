using Microsoft.EntityFrameworkCore;
using lrfdev.auth.api.Models;

namespace lrfdev.auth.api.Data;

public sealed class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<PermissionGroup> Groups => Set<PermissionGroup>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserGroup> UserGroups => Set<UserGroup>();
    public DbSet<UserPermission> UserPermissions => Set<UserPermission>();
    public DbSet<GroupPermission> GroupPermissions => Set<GroupPermission>();
    public DbSet<OAuthClient> OAuthClients => Set<OAuthClient>();
    public DbSet<OAuthAuthorizationCode> OAuthAuthorizationCodes => Set<OAuthAuthorizationCode>();
    public DbSet<OAuthDeviceFlowSession> OAuthDeviceFlowSessions => Set<OAuthDeviceFlowSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Username).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(200).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
            entity.HasIndex(x => x.Username).IsUnique();
            entity.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<PermissionGroup>(entity =>
        {
            entity.ToTable("permission_groups");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("permissions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(300).IsRequired();
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<UserGroup>(entity =>
        {
            entity.ToTable("user_groups");
            entity.HasKey(x => new { x.UserId, x.GroupId });

            entity.HasOne(x => x.User)
                .WithMany(x => x.UserGroups)
                .HasForeignKey(x => x.UserId);

            entity.HasOne(x => x.Group)
                .WithMany(x => x.UserGroups)
                .HasForeignKey(x => x.GroupId);
        });

        modelBuilder.Entity<UserPermission>(entity =>
        {
            entity.ToTable("user_permissions");
            entity.HasKey(x => new { x.UserId, x.PermissionId });

            entity.HasOne(x => x.User)
                .WithMany(x => x.UserPermissions)
                .HasForeignKey(x => x.UserId);

            entity.HasOne(x => x.Permission)
                .WithMany(x => x.UserPermissions)
                .HasForeignKey(x => x.PermissionId);
        });

        modelBuilder.Entity<GroupPermission>(entity =>
        {
            entity.ToTable("group_permissions");
            entity.HasKey(x => new { x.GroupId, x.PermissionId });

            entity.HasOne(x => x.Group)
                .WithMany(x => x.GroupPermissions)
                .HasForeignKey(x => x.GroupId);

            entity.HasOne(x => x.Permission)
                .WithMany(x => x.GroupPermissions)
                .HasForeignKey(x => x.PermissionId);
        });

        modelBuilder.Entity<OAuthClient>(entity =>
        {
            entity.ToTable("oauth_clients");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ClientId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.ClientSecretHash).HasMaxLength(512);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.RedirectUris).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.AllowedScopes).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.AllowDeviceAuthorization).HasDefaultValue(false);
            entity.HasIndex(x => x.ClientId).IsUnique();
        });

        modelBuilder.Entity<OAuthAuthorizationCode>(entity =>
        {
            entity.ToTable("oauth_authorization_codes");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).HasMaxLength(300).IsRequired();
            entity.Property(x => x.RedirectUri).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.Scope).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.CodeChallenge).HasMaxLength(200).IsRequired();
            entity.Property(x => x.CodeChallengeMethod).HasMaxLength(20).IsRequired();
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => x.ExpiresAtUtc);

            entity.HasOne(x => x.Client)
                .WithMany()
                .HasForeignKey(x => x.ClientId);

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<OAuthDeviceFlowSession>(entity =>
        {
            entity.ToTable("oauth_device_flow_sessions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.DeviceCode).HasMaxLength(500).IsRequired();
            entity.Property(x => x.UserCode).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Scope).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(32).IsRequired();
            entity.HasIndex(x => x.DeviceCode).IsUnique();
            entity.HasIndex(x => x.UserCode).IsUnique();
            entity.HasIndex(x => x.ExpiresAtUtc);

            entity.HasOne(x => x.Client)
                .WithMany()
                .HasForeignKey(x => x.ClientId);

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId);
        });
    }
}

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
    }
}


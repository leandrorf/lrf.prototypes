using IdentityServer.Models;
using Microsoft.EntityFrameworkCore;

namespace IdentityServer.Data;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

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
    }
}


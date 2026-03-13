using IdentityServer.Models;
using Microsoft.EntityFrameworkCore;

namespace IdentityServer.Data;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var user = modelBuilder.Entity<User>();

        user.HasIndex(u => u.UserName).IsUnique();
        user.Property(u => u.UserName).IsRequired().HasMaxLength(100);
        user.Property(u => u.PasswordHash).IsRequired().HasMaxLength(256);
        user.Property(u => u.PasswordSalt).IsRequired().HasMaxLength(128);
        user.Property(u => u.Email).HasMaxLength(256);
    }
}


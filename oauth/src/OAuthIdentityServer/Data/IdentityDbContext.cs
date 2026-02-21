using Microsoft.EntityFrameworkCore;
using OAuthIdentityServer.Models;

namespace OAuthIdentityServer.Data;

public class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<AuthorizationCode> AuthorizationCodes => Set<AuthorizationCode>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Subject).IsUnique();
            e.HasIndex(x => x.NormalizedUserName).IsUnique();
            e.Property(x => x.UserName).HasMaxLength(256);
            e.Property(x => x.NormalizedUserName).HasMaxLength(256);
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.NormalizedEmail).HasMaxLength(256);
            e.Property(x => x.Subject).HasMaxLength(64);
        });

        modelBuilder.Entity<Client>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ClientId).IsUnique();
            e.Property(x => x.ClientId).HasMaxLength(128);
            e.Property(x => x.ClientName).HasMaxLength(200);
            e.Property(x => x.RedirectUris).HasMaxLength(2000);
            e.Property(x => x.AllowedGrantTypes).HasMaxLength(200);
            e.Property(x => x.AllowedScopes).HasMaxLength(500);
        });

        modelBuilder.Entity<AuthorizationCode>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Code).HasMaxLength(256);
            e.Property(x => x.ClientId).HasMaxLength(128);
            e.Property(x => x.UserSubject).HasMaxLength(64);
            e.Property(x => x.RedirectUri).HasMaxLength(2000);
            e.Property(x => x.CodeChallenge).HasMaxLength(256);
            e.Property(x => x.CodeChallengeMethod).HasMaxLength(10);
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Token).IsUnique();
            e.Property(x => x.Token).HasMaxLength(256);
            e.Property(x => x.ClientId).HasMaxLength(128);
            e.Property(x => x.UserSubject).HasMaxLength(64);
        });
    }
}

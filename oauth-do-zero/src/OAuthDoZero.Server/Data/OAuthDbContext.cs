using Microsoft.EntityFrameworkCore;
using OAuthDoZero.Server.Models;

namespace OAuthDoZero.Server.Data;

/// <summary>
/// Contexto do banco de dados para OAuth/OpenID Connect
/// </summary>
public class OAuthDbContext : DbContext
{
    public OAuthDbContext(DbContextOptions<OAuthDbContext> options) : base(options)
    {
    }

    // Entidades OAuth/OpenID Connect
    public DbSet<User> Users { get; set; }
    public DbSet<Client> Clients { get; set; }
    public DbSet<Scope> Scopes { get; set; }
    public DbSet<AuthorizationCode> AuthorizationCodes { get; set; }
    public DbSet<AccessToken> AccessTokens { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUserEntity(modelBuilder);
        ConfigureClientEntity(modelBuilder);
        ConfigureScopeEntity(modelBuilder);
        ConfigureAuthorizationCodeEntity(modelBuilder);
        ConfigureAccessTokenEntity(modelBuilder);
        ConfigureRefreshTokenEntity(modelBuilder);

        SeedData(modelBuilder);
    }

    private void ConfigureUserEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.IsActive);
            
            entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.Salt).IsRequired();
        });
    }

    private void ConfigureClientEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasKey(e => e.ClientId);
            entity.HasIndex(e => e.IsActive);
            
            entity.Property(e => e.ClientId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ClientName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ClientSecret).IsRequired();
            entity.Property(e => e.ClientType).IsRequired().HasMaxLength(20);
        });
    }

    private void ConfigureScopeEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Scope>(entity =>
        {
            entity.HasKey(e => e.Name);
            entity.HasIndex(e => e.IsActive);
            
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(20);
        });
    }

    private void ConfigureAuthorizationCodeEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuthorizationCode>(entity =>
        {
            entity.HasKey(e => e.Code);
            entity.HasIndex(e => new { e.UserId, e.ClientId });
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => e.IsUsed);
            
            entity.Property(e => e.Code).IsRequired().HasMaxLength(100);
            entity.Property(e => e.RedirectUri).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Scopes).IsRequired().HasMaxLength(1000);
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.AuthorizationCodes)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Client)
                .WithMany(c => c.AuthorizationCodes)
                .HasForeignKey(e => e.ClientId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureAccessTokenEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccessToken>(entity =>
        {
            entity.HasKey(e => e.TokenId);
            entity.HasIndex(e => e.TokenHash);
            entity.HasIndex(e => new { e.UserId, e.ClientId });
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => e.IsRevoked);
            entity.HasIndex(e => e.JwtId);
            
            entity.Property(e => e.TokenId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.TokenHash).IsRequired();
            entity.Property(e => e.Scopes).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.TokenType).IsRequired().HasMaxLength(20);
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.AccessTokens)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Client)
                .WithMany(c => c.AccessTokens)
                .HasForeignKey(e => e.ClientId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureRefreshTokenEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.TokenId);
            entity.HasIndex(e => e.TokenHash);
            entity.HasIndex(e => new { e.UserId, e.ClientId });
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => new { e.IsUsed, e.IsRevoked });
            
            entity.Property(e => e.TokenId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.TokenHash).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Scopes).IsRequired().HasMaxLength(1000);
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Client)
                .WithMany(c => c.RefreshTokens)
                .HasForeignKey(e => e.ClientId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.AccessToken)
                .WithOne(at => at.RefreshToken)
                .HasForeignKey<RefreshToken>(e => e.AccessTokenId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    /// <summary>
    /// Seed de dados iniciais
    /// </summary>
    private void SeedData(ModelBuilder modelBuilder)
    {
        // Escopos padrão do OpenID Connect
        modelBuilder.Entity<Scope>().HasData(
            new Scope
            {
                Name = StandardScopes.OpenId,
                DisplayName = "OpenID",
                Description = "Permite autenticação usando OpenID Connect",
                Type = "identity",
                AssociatedClaims = """["sub"]""",
                Required = true,
                ShowInConsentScreen = false
            },
            new Scope
            {
                Name = StandardScopes.Profile,
                DisplayName = "Perfil",
                Description = "Acesso às informações do seu perfil",
                Type = "identity",
                AssociatedClaims = """["name", "given_name", "family_name", "preferred_username", "picture", "website", "gender", "birthdate", "zoneinfo", "locale", "updated_at"]""",
                ShowInConsentScreen = true
            },
            new Scope
            {
                Name = StandardScopes.Email,
                DisplayName = "Email",
                Description = "Acesso ao seu endereço de email",
                Type = "identity",
                AssociatedClaims = """["email", "email_verified"]""",
                ShowInConsentScreen = true,
                Emphasize = true
            },
            new Scope
            {
                Name = StandardScopes.OfflineAccess,
                DisplayName = "Acesso offline",
                Description = "Permite acesso às suas informações mesmo quando você estiver offline",
                Type = "resource",
                ShowInConsentScreen = true,
                Emphasize = true
            }
        );
        
        // Cliente de demonstração
        modelBuilder.Entity<Client>().HasData(
            new Client
            {
                ClientId = "demo-spa",
                ClientName = "Aplicação Demo SPA",
                Description = "Aplicação de demonstração Single Page App",
                ClientSecret = "$2a$12$1ePlOPG5M8N7X7K8M7K8MeHw...", // Hash de "demo-secret"
                ClientType = "public",
                GrantTypes = "authorization_code",
                ResponseTypes = "code",
                RedirectUris = "http://localhost:3000/callback,https://localhost:3000/callback",
                PostLogoutRedirectUris = "http://localhost:3000,https://localhost:3000",
                AllowedScopes = "openid profile email offline_access",
                RequirePkce = true,
                AllowPlainTextPkce = false,
                AccessTokenLifetime = 3600,
                RefreshTokenLifetime = 2592000,
                IsActive = true
            }
        );
    }
}
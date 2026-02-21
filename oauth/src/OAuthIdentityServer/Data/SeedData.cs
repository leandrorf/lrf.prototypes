using Microsoft.EntityFrameworkCore;
using OAuthIdentityServer.Models;

namespace OAuthIdentityServer.Data;

/// <summary>
/// Dados iniciais para desenvolvimento: um usuário e um cliente de teste.
/// Execute uma vez após as migrations (ou chame de Program.cs em desenvolvimento).
/// </summary>
public static class SeedData
{
    public static async Task EnsureSeedAsync(IdentityDbContext db)
    {
        if (await db.Users.AnyAsync())
            return;

        var user = new User
        {
            Subject = Guid.NewGuid().ToString("N"),
            UserName = "admin",
            NormalizedUserName = "ADMIN",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            Email = "admin@example.com",
            NormalizedEmail = "ADMIN@EXAMPLE.COM",
            EmailConfirmed = true,
            Name = "Administrador",
            GivenName = "Admin",
            FamilyName = "Sistema",
            IsActive = true
        };
        db.Users.Add(user);

        var clientSecretHash = BCrypt.Net.BCrypt.HashPassword("secret");
        var client = new Client
        {
            ClientId = "demo-client",
            ClientSecretHash = clientSecretHash,
            ClientName = "Cliente Demo",
            RequireClientSecret = true,
            IsEnabled = true,
            RedirectUris = "https://localhost:5001/callback,http://localhost:5000/callback,https://oidcdebugger.com/debug",
            AllowedGrantTypes = "authorization_code,refresh_token",
            AllowedScopes = "openid,profile,email,offline_access",
            AllowOfflineAccess = true,
            RequirePkce = true,
            AccessTokenLifetimeMinutes = 60,
            RefreshTokenLifetimeDays = 30
        };
        db.Clients.Add(client);

        await db.SaveChangesAsync();
    }
}

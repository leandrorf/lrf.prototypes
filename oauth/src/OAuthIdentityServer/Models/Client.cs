namespace OAuthIdentityServer.Models;

public class Client
{
    public int Id { get; set; }
    public string ClientId { get; set; } = null!;
    public string? ClientSecretHash { get; set; } // Hash do secret para clientes confidenciais
    public string? ClientName { get; set; }
    public bool RequireClientSecret { get; set; } = true;
    public bool IsEnabled { get; set; } = true;

    /// <summary>Redirect URIs separados por vírgula (allowed callbacks).</summary>
    public string RedirectUris { get; set; } = null!;

    /// <summary>Post logout redirect URIs separados por vírgula.</summary>
    public string? PostLogoutRedirectUris { get; set; }

    /// <summary>Grant types permitidos: authorization_code, client_credentials, refresh_token.</summary>
    public string AllowedGrantTypes { get; set; } = "authorization_code,refresh_token";

    /// <summary>Scopes permitidos: openid, profile, email, etc.</summary>
    public string AllowedScopes { get; set; } = "openid,profile,email";

    public bool AllowOfflineAccess { get; set; } = true; // refresh_token
    public int AccessTokenLifetimeMinutes { get; set; } = 60;
    public int RefreshTokenLifetimeDays { get; set; } = 30;
    public bool RequirePkce { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

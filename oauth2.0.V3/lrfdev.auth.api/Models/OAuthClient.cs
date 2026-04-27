namespace lrfdev.auth.api.Models;

public sealed class OAuthClient
{
    public Guid Id { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string? ClientSecretHash { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RedirectUris { get; set; } = string.Empty;
    public string AllowedScopes { get; set; } = string.Empty;
    public bool RequirePkce { get; set; } = true;
    public bool IsConfidentialClient { get; set; }
    /// <summary>Permite fluxo Device Authorization (RFC 8628), ex.: TV / IoT.</summary>
    public bool AllowDeviceAuthorization { get; set; }
    public bool IsActive { get; set; } = true;
}

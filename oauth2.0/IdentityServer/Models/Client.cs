using System.ComponentModel.DataAnnotations;

namespace IdentityServer.Models;

/// <summary>Cliente OAuth 2.0 (aplicação que usa o servidor de identidade).</summary>
public class Client
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string ClientId { get; set; } = default!;

    [Required]
    [MaxLength(256)]
    public string ClientSecretHash { get; set; } = default!;

    [Required]
    [MaxLength(128)]
    public string ClientSecretSalt { get; set; } = default!;

    [MaxLength(200)]
    public string? Name { get; set; }

    /// <summary>URIs de redirecionamento permitidas, separadas por vírgula.</summary>
    [Required]
    [MaxLength(2000)]
    public string RedirectUris { get; set; } = default!;

    /// <summary>Tipos de grant permitidos: authorization_code, refresh_token.</summary>
    [Required]
    [MaxLength(200)]
    public string AllowedGrantTypes { get; set; } = "authorization_code,refresh_token";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

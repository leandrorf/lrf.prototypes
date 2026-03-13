using System.ComponentModel.DataAnnotations;

namespace IdentityServer.Models;

/// <summary>Request para registrar um cliente OAuth (uso em desenvolvimento/setup).</summary>
public record RegisterClientRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(200)]
    public string ClientId { get; init; } = default!;

    [Required]
    [MinLength(1)]
    public string ClientSecret { get; init; } = default!;

    [MaxLength(200)]
    public string? Name { get; init; }

    /// <summary>URIs de redirecionamento separadas por vírgula (ex: https://app.example.com/callback,https://localhost:3000/callback).</summary>
    [Required]
    [MinLength(1)]
    [MaxLength(2000)]
    public string RedirectUris { get; init; } = default!;
}

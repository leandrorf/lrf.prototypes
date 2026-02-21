using System.ComponentModel.DataAnnotations;

namespace OAuthDoZero.Server.DTOs;

/// <summary>
/// Requisição para obter tokens OAuth 2.0
/// </summary>
public class TokenRequest
{
    [Required]
    public string GrantType { get; set; } = string.Empty; // "authorization_code", "refresh_token"

    [Required]
    public string ClientId { get; set; } = string.Empty;

    public string? ClientSecret { get; set; }

    // Para authorization_code grant
    public string? Code { get; set; }
    public string? RedirectUri { get; set; }

    // Para refresh_token grant
    public string? RefreshToken { get; set; }

    // PKCE
    public string? CodeVerifier { get; set; }

    // Scopes para refresh
    public string? Scope { get; set; }
}
using System.ComponentModel.DataAnnotations;

namespace OAuthDoZero.Server.DTOs;

/// <summary>
/// Requisição de autorização OAuth 2.0 / OpenID Connect
/// </summary>
public class AuthorizationRequest
{
    [Required]
    public string ResponseType { get; set; } = string.Empty; // "code"

    [Required]
    public string ClientId { get; set; } = string.Empty;

    public string? RedirectUri { get; set; }

    public string? Scope { get; set; } // "openid profile email"

    public string? State { get; set; }

    // PKCE Parameters
    public string? CodeChallenge { get; set; }
    public string? CodeChallengeMethod { get; set; } = "S256";

    // OpenID Connect Parameters
    public string? Nonce { get; set; }
    public string? Prompt { get; set; }
    public string? MaxAge { get; set; }
    public string? LoginHint { get; set; }
}
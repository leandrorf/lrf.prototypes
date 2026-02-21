using System.Text.Json.Serialization;

namespace OAuthDoZero.Server.DTOs;

/// <summary>
/// Resposta do endpoint de token OAuth 2.0
/// </summary>
public class TokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "Bearer";

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    // OpenID Connect
    [JsonPropertyName("id_token")]
    public string? IdToken { get; set; }
}

/// <summary>
/// Resposta de erro OAuth 2.0
/// </summary>
public class OAuthErrorResponse
{
    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;

    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; set; }

    [JsonPropertyName("error_uri")]
    public string? ErrorUri { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }
}
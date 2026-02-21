using System.Text.Json.Serialization;

namespace OAuthDoZero.Server.DTOs;

/// <summary>
/// Resposta do endpoint UserInfo (OpenID Connect)
/// </summary>
public class UserInfoResponse
{
    [JsonPropertyName("sub")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("given_name")]
    public string? GivenName { get; set; }

    [JsonPropertyName("family_name")]
    public string? FamilyName { get; set; }

    [JsonPropertyName("preferred_username")]
    public string? PreferredUsername { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("email_verified")]
    public bool EmailVerified { get; set; }

    [JsonPropertyName("picture")]
    public string? Picture { get; set; }

    [JsonPropertyName("locale")]
    public string? Locale { get; set; }

    [JsonPropertyName("zoneinfo")]
    public string? TimeZone { get; set; }

    [JsonPropertyName("updated_at")]
    public long? UpdatedAt { get; set; }
}
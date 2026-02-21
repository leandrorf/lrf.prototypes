using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OAuthDoZero.Server.Models;

/// <summary>
/// Representa um escopo OAuth 2.0 / OpenID Connect
/// </summary>
[Table("Scopes")]
public class Scope
{
    [Key]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    /// <summary>
    /// Tipo do escopo: api, identity, resource
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Type { get; set; } = "api"; // api, identity, resource
    
    /// <summary>
    /// Claims associados a este escopo (JSON array)
    /// </summary>
    [Column(TypeName = "TEXT")]
    public string? AssociatedClaims { get; set; }
    
    /// <summary>
    /// Se é um escopo obrigatório (sempre incluído)
    /// </summary>
    public bool Required { get; set; } = false;
    
    /// <summary>
    /// Se deve aparecer na tela de consentimento
    /// </summary>
    public bool ShowInConsentScreen { get; set; } = true;
    
    /// <summary>
    /// Se enfatizar na tela de consentimento
    /// </summary>
    public bool Emphasize { get; set; } = false;
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Escopos padrão do OpenID Connect
/// </summary>
public static class StandardScopes
{
    public const string OpenId = "openid";
    public const string Profile = "profile";
    public const string Email = "email";
    public const string Address = "address";
    public const string Phone = "phone";
    public const string OfflineAccess = "offline_access";
}

/// <summary>
/// Claims padrão do OpenID Connect
/// </summary>
public static class StandardClaims
{
    // OpenID Connect Core claims
    public const string Subject = "sub";
    public const string Name = "name";
    public const string GivenName = "given_name";
    public const string FamilyName = "family_name";
    public const string MiddleName = "middle_name";
    public const string Nickname = "nickname";
    public const string PreferredUsername = "preferred_username";
    public const string Profile = "profile";
    public const string Picture = "picture";
    public const string Website = "website";
    public const string Email = "email";
    public const string EmailVerified = "email_verified";
    public const string Gender = "gender";
    public const string Birthdate = "birthdate";
    public const string ZoneInfo = "zoneinfo";
    public const string Locale = "locale";
    public const string PhoneNumber = "phone_number";
    public const string PhoneNumberVerified = "phone_number_verified";
    public const string Address = "address";
    public const string UpdatedAt = "updated_at";
    
    // JWT Standard claims
    public const string Issuer = "iss";
    public const string Audience = "aud";
    public const string Expiration = "exp";
    public const string NotBefore = "nbf";
    public const string IssuedAt = "iat";
    public const string JwtId = "jti";
    
    // OAuth 2.0 claims
    public const string ClientId = "client_id";
    public const string Scope = "scope";
}
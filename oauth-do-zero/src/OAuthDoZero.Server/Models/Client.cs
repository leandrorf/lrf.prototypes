using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OAuthDoZero.Server.Models;

/// <summary>
/// Representa um cliente OAuth 2.0 (aplicação)
/// </summary>
[Table("Clients")]
public class Client
{
    [Key]
    [MaxLength(100)]
    public string ClientId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string ClientName { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [Required]
    public string ClientSecret { get; set; } = string.Empty; // Hash do secret
    
    /// <summary>
    /// Tipos de cliente: public (SPA, Mobile) ou confidential (Server-to-Server)
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string ClientType { get; set; } = "confidential"; // public, confidential
    
    /// <summary>
    /// Grant types permitidos (authorization_code, client_credentials, etc.)
    /// </summary>
    [Required]
    public string GrantTypes { get; set; } = "authorization_code"; // JSON array ou delimitado por vírgulas
    
    /// <summary>
    /// Response types permitidos para Authorization Code Flow
    /// </summary>
    [Required]
    public string ResponseTypes { get; set; } = "code"; // code, code id_token, etc.
    
    /// <summary>
    /// URIs de redirecionamento válidos (separados por vírgula)
    /// </summary>
    [Required]
    [Column(TypeName = "TEXT")]
    public string RedirectUris { get; set; } = string.Empty;
    
    /// <summary>
    /// URIs de logout válidos (separados por vírgula)
    /// </summary>
    [Column(TypeName = "TEXT")]
    public string? PostLogoutRedirectUris { get; set; }
    
    /// <summary>
    /// Escopos permitidos para este cliente (separados por espaço)
    /// </summary>
    [Required]
    public string AllowedScopes { get; set; } = "openid profile email";
    
    /// <summary>
    /// Tempo de vida do access token em segundos
    /// </summary>
    public int AccessTokenLifetime { get; set; } = 3600; // 1 hora
    
    /// <summary>
    /// Tempo de vida do authorization code em segundos
    /// </summary>
    public int AuthorizationCodeLifetime { get; set; } = 300; // 5 minutos
    
    /// <summary>
    /// Tempo de vida do refresh token em segundos
    /// </summary>
    public int RefreshTokenLifetime { get; set; } = 2592000; // 30 dias
    
    /// <summary>
    /// Se permite refresh token
    /// </summary>
    public bool AllowRefreshToken { get; set; } = true;
    
    /// <summary>
    /// Requer PKCE (Proof Key for Code Exchange)
    /// </summary>
    public bool RequirePkce { get; set; } = true;
    
    /// <summary>
    /// Permite plain text PKCE
    /// </summary>
    public bool AllowPlainTextPkce { get; set; } = false;
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties
    public virtual ICollection<AuthorizationCode> AuthorizationCodes { get; set; } = new List<AuthorizationCode>();
    public virtual ICollection<AccessToken> AccessTokens { get; set; } = new List<AccessToken>();
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OAuthDoZero.Server.Models;

/// <summary>
/// Representa um Access Token OAuth 2.0
/// </summary>
[Table("AccessTokens")]
public class AccessToken
{
    [Key]
    [MaxLength(100)]
    public string TokenId { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// JWT token hasheado para verificação
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string TokenHash { get; set; } = string.Empty;
    
    [Required]
    [ForeignKey(nameof(User))]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    [ForeignKey(nameof(Client))]
    [MaxLength(100)]
    public string ClientId { get; set; } = string.Empty;
    
    /// <summary>
    /// Escopos concedidos (separados por espaço)
    /// </summary>
    [Required]
    [MaxLength(1000)]
    public string Scopes { get; set; } = string.Empty;
    
    /// <summary>
    /// Claims incluídos no token (JSON)
    /// </summary>
    [Column(TypeName = "TEXT")]
    public string? Claims { get; set; }
    
    /// <summary>
    /// Tipo do token (Bearer)
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string TokenType { get; set; } = "Bearer";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime ExpiresAt { get; set; }
    
    public bool IsRevoked { get; set; } = false;
    
    public DateTime? RevokedAt { get; set; }
    
    /// <summary>
    /// JWT ID (jti claim)
    /// </summary>
    [MaxLength(100)]
    public string? JwtId { get; set; }
    
    /// <summary>
    /// Audience (aud claim)
    /// </summary>
    [MaxLength(500)]
    public string? Audience { get; set; }

    // Navigation Properties
    public virtual User User { get; set; } = null!;
    public virtual Client Client { get; set; } = null!;
    public virtual RefreshToken? RefreshToken { get; set; }
    
    /// <summary>
    /// Verifica se o token ainda é válido
    /// </summary>
    public bool IsValid => !IsRevoked && DateTime.UtcNow < ExpiresAt;
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OAuthDoZero.Server.Models;

/// <summary>
/// Representa um Refresh Token OAuth 2.0
/// </summary>
[Table("RefreshTokens")]
public class RefreshToken
{
    [Key]
    [MaxLength(100)]
    public string TokenId { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Token hasheado para verificação
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
    
    [ForeignKey(nameof(AccessToken))]
    [MaxLength(100)]
    public string? AccessTokenId { get; set; }
    
    /// <summary>
    /// Escopos originais (separados por espaço)
    /// </summary>
    [Required]
    [MaxLength(1000)]
    public string Scopes { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime ExpiresAt { get; set; }
    
    public bool IsUsed { get; set; } = false;
    
    public DateTime? UsedAt { get; set; }
    
    public bool IsRevoked { get; set; } = false;
    
    public DateTime? RevokedAt { get; set; }

    // Navigation Properties
    public virtual User User { get; set; } = null!;
    public virtual Client Client { get; set; } = null!;
    public virtual AccessToken? AccessToken { get; set; }
    
    /// <summary>
    /// Verifica se o refresh token ainda é válido
    /// </summary>
    public bool IsValid => !IsUsed && !IsRevoked && DateTime.UtcNow < ExpiresAt;
}
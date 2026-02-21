using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OAuthDoZero.Server.Models;

/// <summary>
/// Representa um código de autorização OAuth 2.0
/// </summary>
[Table("AuthorizationCodes")]
public class AuthorizationCode
{
    [Key]
    [MaxLength(100)]
    public string Code { get; set; } = string.Empty;
    
    [Required]
    [ForeignKey(nameof(User))]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    [ForeignKey(nameof(Client))]
    [MaxLength(100)]
    public string ClientId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string RedirectUri { get; set; } = string.Empty;
    
    /// <summary>
    /// Escopos solicitados (separados por espaço)
    /// </summary>
    [Required]
    [MaxLength(1000)]
    public string Scopes { get; set; } = string.Empty;
    
    /// <summary>
    /// State parameter para CSRF protection
    /// </summary>
    [MaxLength(500)]
    public string? State { get; set; }
    
    /// <summary>
    /// PKCE Code Challenge
    /// </summary>
    [MaxLength(128)]
    public string? CodeChallenge { get; set; }
    
    /// <summary>
    /// PKCE Code Challenge Method (S256 ou plain)
    /// </summary>
    [MaxLength(10)]
    public string? CodeChallengeMethod { get; set; }
    
    /// <summary>
    /// Nonce para OpenID Connect
    /// </summary>
    [MaxLength(200)]
    public string? Nonce { get; set; }
    
    /// <summary>
    /// Claims solicitados (JSON)
    /// </summary>
    [Column(TypeName = "TEXT")]
    public string? RequestedClaims { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime ExpiresAt { get; set; }
    
    public bool IsUsed { get; set; } = false;
    
    public DateTime? UsedAt { get; set; }

    // Navigation Properties
    public virtual User User { get; set; } = null!;
    public virtual Client Client { get; set; } = null!;
    
    /// <summary>
    /// Verifica se o código ainda é válido
    /// </summary>
    public bool IsValid => !IsUsed && DateTime.UtcNow < ExpiresAt;
}
using System.ComponentModel.DataAnnotations;

namespace identityserver.api.Models;

/// <summary>Código de autorização OAuth 2.0 (uso único, vida curta).</summary>
public class AuthorizationCode
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = default!;
    public int ClientId { get; set; }
    public Client Client { get; set; } = default!;

    [Required]
    [MaxLength(256)]
    public string Code { get; set; } = default!;

    [Required]
    [MaxLength(2000)]
    public string RedirectUri { get; set; } = default!;

    [MaxLength(500)]
    public string? Scope { get; set; }

    [MaxLength(500)]
    public string? State { get; set; }

    /// <summary>PKCE: code_challenge enviado no /authorize.</summary>
    [MaxLength(512)]
    public string? CodeChallenge { get; set; }

    /// <summary>PKCE: S256 ou plain.</summary>
    [MaxLength(10)]
    public string? CodeChallengeMethod { get; set; }

    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UsedAtUtc { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;
    public bool IsUsed => UsedAtUtc.HasValue;
    public bool IsValid => !IsUsed && !IsExpired;
}

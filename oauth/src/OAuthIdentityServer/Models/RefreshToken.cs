namespace OAuthIdentityServer.Models;

public class RefreshToken
{
    public int Id { get; set; }
    public string Token { get; set; } = null!;
    public string ClientId { get; set; } = null!;
    public string UserSubject { get; set; } = null!;
    public string? Scope { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UsedAt { get; set; }
    public bool IsRevoked { get; set; }
}

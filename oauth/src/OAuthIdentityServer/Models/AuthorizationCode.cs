namespace OAuthIdentityServer.Models;

public class AuthorizationCode
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string ClientId { get; set; } = null!;
    public string UserSubject { get; set; } = null!;
    public string RedirectUri { get; set; } = null!;
    public string? Scope { get; set; }
    public string? CodeChallenge { get; set; }
    public string? CodeChallengeMethod { get; set; } // S256 ou plain
    public string? Nonce { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

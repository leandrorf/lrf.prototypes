namespace lrfdev.auth.api.Models;

public sealed class OAuthAuthorizationCode
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public Guid ClientId { get; set; }
    public OAuthClient Client { get; set; } = default!;
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;
    public string RedirectUri { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public string CodeChallenge { get; set; } = string.Empty;
    public string CodeChallengeMethod { get; set; } = "S256";
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }
}

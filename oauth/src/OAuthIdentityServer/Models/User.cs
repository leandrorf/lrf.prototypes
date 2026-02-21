namespace OAuthIdentityServer.Models;

public class User
{
    public int Id { get; set; }
    public string Subject { get; set; } = null!; // Identificador único OIDC (ex: GUID)
    public string UserName { get; set; } = null!;
    public string NormalizedUserName { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? NormalizedEmail { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? Name { get; set; }
    public string? GivenName { get; set; }
    public string? FamilyName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

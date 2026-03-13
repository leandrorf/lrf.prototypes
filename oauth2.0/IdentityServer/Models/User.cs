using System.ComponentModel.DataAnnotations;

namespace IdentityServer.Models;

public class User
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string UserName { get; set; } = default!;

    [Required]
    [MaxLength(256)]
    public string PasswordHash { get; set; } = default!;

    [Required]
    [MaxLength(128)]
    public string PasswordSalt { get; set; } = default!;

    [MaxLength(256)]
    public string? Email { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}


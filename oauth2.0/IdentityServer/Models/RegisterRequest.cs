using System.ComponentModel.DataAnnotations;

namespace IdentityServer.Models;

public record RegisterRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public string UserName { get; init; } = default!;

    [Required]
    [MinLength(1)]
    public string Password { get; init; } = default!;

    [MaxLength(256)]
    [EmailAddress]
    public string? Email { get; init; }
}

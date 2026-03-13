using System.ComponentModel.DataAnnotations;

namespace IdentityServer.Models;

public record LoginRequest
{
    [Required]
    public string UserName { get; init; } = default!;

    [Required]
    public string Password { get; init; } = default!;
}

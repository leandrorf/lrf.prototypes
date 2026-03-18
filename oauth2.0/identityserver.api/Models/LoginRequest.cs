using System.ComponentModel.DataAnnotations;

namespace identityserver.api.Models;

public record LoginRequest
{
    [Required]
    [MinLength(1)]
    public string UserName { get; init; } = default!;

    [Required]
    [MinLength(1)]
    public string Password { get; init; } = default!;
}

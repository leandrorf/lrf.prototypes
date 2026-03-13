using System.ComponentModel.DataAnnotations;

namespace IdentityServer.Models;

public record RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; init; } = default!;
}

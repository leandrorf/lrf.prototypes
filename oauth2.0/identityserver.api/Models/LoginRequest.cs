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

    /// <summary>ID externo do dispositivo (TV). Se informado, exige regra de acesso TV por grupo do dispositivo.</summary>
    public string? DeviceId { get; init; }
}

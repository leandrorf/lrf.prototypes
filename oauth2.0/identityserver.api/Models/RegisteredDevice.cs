using System.ComponentModel.DataAnnotations;

namespace identityserver.api.Models;

/// <summary>Dispositivo (ex.: TV) identificado por ID externo; pertence a um grupo lógico para regras de login.</summary>
public class RegisteredDevice
{
    public int Id { get; set; }

    [Required]
    [MaxLength(120)]
    public string ExternalId { get; set; } = default!;

    [Required]
    [MaxLength(100)]
    public string DeviceGroup { get; set; } = default!;

    [MaxLength(200)]
    public string? DisplayName { get; set; }
}

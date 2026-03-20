using System.ComponentModel.DataAnnotations;

namespace identityserver.api.Models;

/// <summary>Define que usuários do grupo podem fazer login TV em dispositivos do <see cref="DeviceGroup"/>.</summary>
public class GroupTvAccess
{
    public int Id { get; set; }

    public int GroupId { get; set; }
    public PermissionGroup Group { get; set; } = default!;

    [Required]
    [MaxLength(100)]
    public string DeviceGroup { get; set; } = default!;
}

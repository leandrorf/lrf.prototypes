using System.ComponentModel.DataAnnotations;

namespace identityserver.api.Models;

/// <summary>Grupo de usuários (RBAC): nomes estáveis para claims JWT e regras de TV.</summary>
public class PermissionGroup
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = default!;

    [MaxLength(500)]
    public string? Description { get; set; }

    public ICollection<UserGroupMembership> UserMemberships { get; set; } = new List<UserGroupMembership>();
    public ICollection<GroupFeatureGrant> FeatureGrants { get; set; } = new List<GroupFeatureGrant>();
    public ICollection<GroupTvAccess> TvAccessRules { get; set; } = new List<GroupTvAccess>();
}

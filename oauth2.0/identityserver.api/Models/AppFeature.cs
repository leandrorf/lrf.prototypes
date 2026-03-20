using System.ComponentModel.DataAnnotations;

namespace identityserver.api.Models;

/// <summary>Funcionalidade da aplicação (ex.: app.demo.read); emitida como claim "permission".</summary>
public class AppFeature
{
    public int Id { get; set; }

    [Required]
    [MaxLength(120)]
    public string Code { get; set; } = default!;

    [MaxLength(300)]
    public string? DisplayName { get; set; }

    public ICollection<GroupFeatureGrant> GroupGrants { get; set; } = new List<GroupFeatureGrant>();
}

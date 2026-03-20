namespace lrfdev.auth.api.Models;

public sealed class GroupPermission
{
    public Guid GroupId { get; set; }
    public PermissionGroup Group { get; set; } = default!;

    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; } = default!;
}

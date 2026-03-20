namespace lrfdev.auth.api.Models;

public sealed class PermissionGroup
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public List<UserGroup> UserGroups { get; set; } = [];
    public List<GroupPermission> GroupPermissions { get; set; } = [];
}

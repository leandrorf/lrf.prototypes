namespace lrfdev.auth.api.Models;

public sealed class Permission
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public List<UserPermission> UserPermissions { get; set; } = [];
    public List<GroupPermission> GroupPermissions { get; set; } = [];
}

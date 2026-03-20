namespace lrfdev.auth.api.Models;

public sealed class UserPermission
{
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;

    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; } = default!;
}

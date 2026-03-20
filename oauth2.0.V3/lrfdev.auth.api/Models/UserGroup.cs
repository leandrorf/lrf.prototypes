namespace lrfdev.auth.api.Models;

public sealed class UserGroup
{
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;

    public Guid GroupId { get; set; }
    public PermissionGroup Group { get; set; } = default!;
}

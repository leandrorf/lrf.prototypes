namespace identityserver.api.Models;

public class UserGroupMembership
{
    public int UserId { get; set; }
    public User User { get; set; } = default!;

    public int GroupId { get; set; }
    public PermissionGroup Group { get; set; } = default!;
}

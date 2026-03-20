namespace identityserver.api.Models;

public class GroupFeatureGrant
{
    public int GroupId { get; set; }
    public PermissionGroup Group { get; set; } = default!;

    public int FeatureId { get; set; }
    public AppFeature Feature { get; set; } = default!;
}

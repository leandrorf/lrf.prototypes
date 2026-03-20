namespace identityserver.api.Services;

public interface IPermissionService
{
    Task<IReadOnlyList<string>> GetGroupNamesForUserAsync(int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetFeatureCodesForUserAsync(int userId, CancellationToken cancellationToken = default);
    /// <summary>TV: permite login neste dispositivo se algum grupo do usuário tem acesso ao <see cref="RegisteredDevice.DeviceGroup"/>.</summary>
    Task<bool> CanUserAccessDeviceAsync(int userId, string deviceExternalId, CancellationToken cancellationToken = default);
}

namespace lrfdev.auth.api.Services;

public interface IPermissionResolver
{
    Task<IReadOnlyCollection<string>> ResolveAsync(Guid userId, CancellationToken cancellationToken = default);
}

using SAPFIAI.Application.Common.Models;

namespace SAPFIAI.Application.Common.Interfaces;

public interface IPermitProvisioningService
{
    Task EnsureAuthorizationModelAsync(CancellationToken cancellationToken = default);

    Task SyncUserAsync(PermitUserSyncRequest request, CancellationToken cancellationToken = default);

    Task AssignRoleAsync(string userId, string roleKey, CancellationToken cancellationToken = default);

    Task UnassignRoleAsync(string userId, string roleKey, CancellationToken cancellationToken = default);

    Task DeleteUserAsync(string userId, CancellationToken cancellationToken = default);

    // Management API
    Task CreateRoleAsync(string roleKey, string roleName, CancellationToken cancellationToken = default);

    Task CreateResourceAsync(string resourceKey, string resourceName, IEnumerable<string> actions, CancellationToken cancellationToken = default);

    Task AssignPermissionsToRoleAsync(string roleKey, IEnumerable<string> permissions, CancellationToken cancellationToken = default);

    Task<IEnumerable<PermitRoleDto>> GetRolesAsync(CancellationToken cancellationToken = default);

    Task<IEnumerable<PermitResourceDto>> GetResourcesAsync(CancellationToken cancellationToken = default);
}

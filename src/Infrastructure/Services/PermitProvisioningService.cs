using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PermitSDK.OpenAPI.Models;
using SAPFIAI.Application.Common.Interfaces;
using SAPFIAI.Application.Common.Models;
using SAPFIAI.Domain.Constants;

namespace SAPFIAI.Infrastructure.Services;

public class PermitProvisioningService : IPermitProvisioningService
{
    private readonly PermitAuthorizationService _permit;
    private readonly ILogger<PermitProvisioningService> _logger;
    private readonly string _defaultTenantKey;

    private static readonly Dictionary<string, string[]> RolePermissions = new()
    {
        [PermitConstants.Roles.Admin] = PermitConstants.ManageUsersActions
            .Select(action => $"{PermitConstants.Resources.ManageUsers}:{action}")
            .ToArray(),
        [PermitConstants.Roles.Manager] =
        [
            $"{PermitConstants.Resources.ManageUsers}:{PermitConstants.Actions.Read}",
            $"{PermitConstants.Resources.ManageUsers}:{PermitConstants.Actions.List}"
        ],
        [PermitConstants.Roles.User] = []
    };

    public PermitProvisioningService(
        PermitAuthorizationService permit,
        IConfiguration configuration,
        ILogger<PermitProvisioningService> logger)
    {
        _permit = permit;
        _logger = logger;
        _defaultTenantKey = configuration["Permitio:DefaultTenant"]
            ?? configuration["PermitIo:DefaultTenant"]
            ?? PermitConstants.Tenants.Default;
    }

    public async Task EnsureAuthorizationModelAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await EnsureTenantAsync();
        await EnsureResourceAsync(PermitConstants.Resources.ManageUsers, PermitConstants.ManageUsersActions);

        foreach (var (roleKey, permissions) in RolePermissions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await EnsureRoleAsync(roleKey);
            await EnsureRolePermissionsAsync(roleKey, permissions);
        }
    }

    public async Task SyncUserAsync(PermitUserSyncRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var email = IsInternetEmail(request.Email) ? request.Email : null;

        await _permit.Client.Api.SyncUser(new UserCreate
        {
            Key = request.UserId,
            Email = email
        });

        _logger.LogInformation("User {UserId} synced to Permit.io.", request.UserId);
    }

    public async Task AssignRoleAsync(string userId, string roleKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _permit.Client.Api.AssignRole(userId, roleKey, _defaultTenantKey);
        _logger.LogInformation("Role {RoleKey} assigned to user {UserId}.", roleKey, userId);
    }

    public async Task UnassignRoleAsync(string userId, string roleKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _permit.Client.Api.UnassignRole(userId, roleKey, _defaultTenantKey);
        _logger.LogInformation("Role {RoleKey} unassigned from user {UserId}.", roleKey, userId);
    }

    public async Task DeleteUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _permit.Client.Api.DeleteUser(userId);
        _logger.LogInformation("User {UserId} deleted from Permit.io.", userId);
    }

    public async Task CreateRoleAsync(string roleKey, string roleName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _permit.Client.Api.CreateRole(new RoleCreate { Key = roleKey, Name = roleName });
        _logger.LogInformation("Role {RoleKey} created in Permit.io.", roleKey);
    }

    public async Task CreateResourceAsync(string resourceKey, string resourceName, IEnumerable<string> actions, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var actionDict = actions.ToDictionary(
            a => a,
            a => new ActionBlockEditable { Name = a });

        await _permit.Client.Api.CreateResource(new ResourceCreate
        {
            Key = resourceKey,
            Name = resourceName,
            Actions = actionDict
        });

        _logger.LogInformation("Resource {ResourceKey} created in Permit.io.", resourceKey);
    }

    public async Task AssignPermissionsToRoleAsync(string roleKey, IEnumerable<string> permissions, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _permit.Client.Api.UpdateRole(roleKey, new RoleUpdate
        {
            Permissions = permissions.ToList()
        });

        _logger.LogInformation("Permissions updated for role {RoleKey}.", roleKey);
    }

    public async Task<IEnumerable<PermitRoleDto>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var paged = await _permit.GetSchemaAsync<PermitPagedResult<PermitRoleRead>>("roles", cancellationToken);
        var result = new List<PermitRoleDto>();
        if (paged?.Data != null)
            foreach (var r in paged.Data)
                result.Add(new PermitRoleDto(r.Key, r.Name ?? r.Key, r.Permissions ?? []));
        return result;
    }

    public async Task<IEnumerable<PermitResourceDto>> GetResourcesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var paged = await _permit.GetSchemaAsync<PermitPagedResult<PermitResourceRead>>("resources", cancellationToken);
        var result = new List<PermitResourceDto>();
        if (paged?.Data != null)
            foreach (var r in paged.Data)
                result.Add(new PermitResourceDto(r.Key, r.Name ?? r.Key, r.Actions?.Keys.ToList() ?? []));
        return result;
    }

    // ── private helpers ──────────────────────────────────────────────────────

    private async Task EnsureTenantAsync()
    {
        try { await _permit.Client.Api.GetTenant(_defaultTenantKey); return; }
        catch { /* not found */ }

        await _permit.Client.Api.CreateTenant(new TenantCreate
        {
            Key = _defaultTenantKey,
            Name = _defaultTenantKey
        });
        _logger.LogInformation("Tenant {TenantKey} created in Permit.io.", _defaultTenantKey);
    }

    private async Task EnsureResourceAsync(string resourceKey, IEnumerable<string> actions)
    {
        try { await _permit.Client.Api.GetResource(resourceKey); return; }
        catch { /* not found */ }

        var actionDict = actions.ToDictionary(a => a, a => new ActionBlockEditable { Name = a });
        await _permit.Client.Api.CreateResource(new ResourceCreate
        {
            Key = resourceKey,
            Name = resourceKey,
            Actions = actionDict
        });
        _logger.LogInformation("Resource {ResourceKey} created in Permit.io.", resourceKey);
    }

    private async Task EnsureRoleAsync(string roleKey)
    {
        try { await _permit.Client.Api.GetRole(roleKey); return; }
        catch { /* not found */ }

        await _permit.Client.Api.CreateRole(new RoleCreate { Key = roleKey, Name = roleKey });
        _logger.LogInformation("Role {RoleKey} created in Permit.io.", roleKey);
    }

    private async Task EnsureRolePermissionsAsync(string roleKey, IEnumerable<string> permissions)
    {
        await _permit.Client.Api.UpdateRole(roleKey, new RoleUpdate
        {
            Permissions = permissions.ToList()
        });
        _logger.LogInformation("Role {RoleKey} permissions synced in Permit.io.", roleKey);
    }

    private static bool IsInternetEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        var atIndex = email.IndexOf('@');
        if (atIndex < 1) return false;
        var domain = email[(atIndex + 1)..];
        return domain.Contains('.') && !domain.EndsWith('.');
    }
}

// REST API response shapes
internal sealed class PermitPagedResult<T>
{
    [System.Text.Json.Serialization.JsonPropertyName("data")]
    public List<T>? Data { get; set; }
}

internal sealed class PermitRoleRead
{
    [System.Text.Json.Serialization.JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string? Name { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("permissions")]
    public List<string>? Permissions { get; set; }
}

internal sealed class PermitResourceRead
{
    [System.Text.Json.Serialization.JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string? Name { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("actions")]
    public Dictionary<string, object>? Actions { get; set; }
}

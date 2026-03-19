using Microsoft.AspNetCore.Mvc;
using SAPFIAI.Application.Common.Interfaces;
using SAPFIAI.Application.Common.Models;
using SAPFIAI.Domain.Constants;

namespace SAPFIAI.Web.Endpoints;

public class PermitManagement : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var group = app.MapGroup(this)
            .WithName("PermitManagement")
            .RequireAuthorization();

        // Roles
        group.MapGet("/roles", GetRoles)
            .WithName("GetPermitRoles")
            .WithSummary("Listar roles en Permit.io")
            .Produces<IEnumerable<PermitRoleDto>>(StatusCodes.Status200OK)
            .RequirePermit(PermitConstants.Resources.PermitManagement, PermitConstants.Actions.Read);

        group.MapPost("/roles", CreateRole)
            .WithName("CreatePermitRole")
            .WithSummary("Crear un rol en Permit.io")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .RequirePermit(PermitConstants.Resources.PermitManagement, PermitConstants.Actions.Create);

        // Resources
        group.MapGet("/resources", GetResources)
            .WithName("GetPermitResources")
            .WithSummary("Listar recursos en Permit.io")
            .Produces<IEnumerable<PermitResourceDto>>(StatusCodes.Status200OK)
            .RequirePermit(PermitConstants.Resources.PermitManagement, PermitConstants.Actions.Read);

        group.MapPost("/resources", CreateResource)
            .WithName("CreatePermitResource")
            .WithSummary("Crear un recurso con acciones en Permit.io")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .RequirePermit(PermitConstants.Resources.PermitManagement, PermitConstants.Actions.Create);

        // Permissions
        group.MapPut("/roles/{roleKey}/permissions", AssignPermissions)
            .WithName("AssignPermissionsToRole")
            .WithSummary("Asignar permisos a un rol (formato: recurso:accion)")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .RequirePermit(PermitConstants.Resources.PermitManagement, PermitConstants.Actions.AssignPermission);

        // User role assignment
        group.MapPost("/users/{userId}/roles/{roleKey}", AssignUserRole)
            .WithName("AssignRoleToUser")
            .WithSummary("Asignar un rol a un usuario en Permit.io")
            .Produces<string>(StatusCodes.Status200OK)
            .RequirePermit(PermitConstants.Resources.PermitManagement, PermitConstants.Actions.AssignRole);

        group.MapDelete("/users/{userId}/roles/{roleKey}", UnassignUserRole)
            .WithName("UnassignRoleFromUser")
            .WithSummary("Quitar un rol a un usuario en Permit.io")
            .Produces<string>(StatusCodes.Status200OK)
            .RequirePermit(PermitConstants.Resources.PermitManagement, PermitConstants.Actions.RemoveRole);
    }

    private static async Task<IResult> GetRoles(IPermitProvisioningService permit, CancellationToken ct)
    {
        var roles = await permit.GetRolesAsync(ct);
        return Results.Ok(roles);
    }

    private static async Task<IResult> CreateRole(
        [FromBody] CreateRoleRequest request,
        IPermitProvisioningService permit,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Key))
            return Results.BadRequest("El campo 'key' es requerido.");

        await permit.CreateRoleAsync(request.Key, request.Name ?? request.Key, ct);
        return Results.Ok($"Rol '{request.Key}' creado en Permit.io.");
    }

    private static async Task<IResult> GetResources(IPermitProvisioningService permit, CancellationToken ct)
    {
        var resources = await permit.GetResourcesAsync(ct);
        return Results.Ok(resources);
    }

    private static async Task<IResult> CreateResource(
        [FromBody] CreateResourceRequest request,
        IPermitProvisioningService permit,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Key))
            return Results.BadRequest("El campo 'key' es requerido.");

        if (request.Actions == null || !request.Actions.Any())
            return Results.BadRequest("Debe especificar al menos una acción.");

        await permit.CreateResourceAsync(request.Key, request.Name ?? request.Key, request.Actions, ct);
        return Results.Ok($"Recurso '{request.Key}' creado en Permit.io.");
    }

    private static async Task<IResult> AssignPermissions(
        string roleKey,
        [FromBody] AssignPermissionsRequest request,
        IPermitProvisioningService permit,
        CancellationToken ct)
    {
        if (request.Permissions == null || !request.Permissions.Any())
            return Results.BadRequest("Debe especificar al menos un permiso (formato: recurso:accion).");

        await permit.AssignPermissionsToRoleAsync(roleKey, request.Permissions, ct);
        return Results.Ok($"Permisos actualizados para el rol '{roleKey}'.");
    }

    private static async Task<IResult> AssignUserRole(
        string userId,
        string roleKey,
        IPermitProvisioningService permit,
        CancellationToken ct)
    {
        await permit.AssignRoleAsync(userId, roleKey, ct);
        return Results.Ok($"Rol '{roleKey}' asignado al usuario '{userId}'.");
    }

    private static async Task<IResult> UnassignUserRole(
        string userId,
        string roleKey,
        IPermitProvisioningService permit,
        CancellationToken ct)
    {
        await permit.UnassignRoleAsync(userId, roleKey, ct);
        return Results.Ok($"Rol '{roleKey}' removido del usuario '{userId}'.");
    }
}

// Request records
public record CreateRoleRequest(string Key, string? Name);
public record CreateResourceRequest(string Key, string? Name, IEnumerable<string> Actions);
public record AssignPermissionsRequest(IEnumerable<string> Permissions);

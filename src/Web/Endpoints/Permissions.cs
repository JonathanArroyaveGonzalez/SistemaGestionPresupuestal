using SAPFIAI.Application.Common.Models;
using SAPFIAI.Application.Permissions.Commands.CreatePermission;
using SAPFIAI.Application.Permissions.Commands.UpdatePermission;
using SAPFIAI.Application.Permissions.Commands.DeletePermission;
using SAPFIAI.Application.Permissions.Commands.AssignPermissionToRole;
using SAPFIAI.Application.Permissions.Commands.RemovePermissionFromRole;
using SAPFIAI.Application.Permissions.Queries.GetPermissions;
using SAPFIAI.Application.Permissions.Queries.GetPermissionById;
using SAPFIAI.Application.Permissions.Queries.GetRolePermissions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SAPFIAI.Web.Infrastructure;

namespace SAPFIAI.Web.Endpoints;

public class Permissions : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var group = app.MapGroup(this)
            .WithName("Permissions")
            .WithOpenApi()
            .RequireAuthorization("RequireAdministrator");

        group.MapGet("/", GetPermissions)
            .WithName("GetPermissions")
            .Produces<List<PermissionDto>>(StatusCodes.Status200OK);

        group.MapGet("/{permissionId:int}", GetPermissionById)
            .WithName("GetPermissionById")
            .Produces<PermissionDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/role/{roleId}", GetRolePermissions)
            .WithName("GetRolePermissions")
            .Produces<List<PermissionDto>>(StatusCodes.Status200OK);

        group.MapPost("/", CreatePermission)
            .WithName("CreatePermission")
            .Produces<int>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPut("/{permissionId:int}", UpdatePermission)
            .WithName("UpdatePermission")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{permissionId:int}", DeletePermission)
            .WithName("DeletePermission")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/assign", AssignPermissionToRole)
            .WithName("AssignPermissionToRole")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPost("/remove", RemovePermissionFromRole)
            .WithName("RemovePermissionFromRole")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> GetPermissions(IMediator mediator, [FromQuery] bool activeOnly = false)
    {
        var permissions = await mediator.Send(new GetPermissionsQuery { ActiveOnly = activeOnly });
        return Results.Ok(permissions);
    }

    private static async Task<IResult> GetPermissionById(IMediator mediator, int permissionId)
    {
        var permission = await mediator.Send(new GetPermissionByIdQuery { PermissionId = permissionId });
        return permission != null ? Results.Ok(permission) : Results.NotFound();
    }

    private static async Task<IResult> GetRolePermissions(IMediator mediator, string roleId)
    {
        var permissions = await mediator.Send(new GetRolePermissionsQuery { RoleId = roleId });
        return Results.Ok(permissions);
    }

    private static async Task<IResult> CreatePermission(IMediator mediator, [FromBody] CreatePermissionCommand command)
    {
        var result = await mediator.Send(command);
        return result.ToCreatedResult(id => $"/api/permissions/{id}");
    }

    private static async Task<IResult> UpdatePermission(IMediator mediator, int permissionId, [FromBody] UpdatePermissionCommand command)
    {
        if (permissionId != command.PermissionId)
            return Results.BadRequest(new { error = "PermissionId mismatch" });

        var result = await mediator.Send(command);
        return result.ToHttpResult();
    }

    private static async Task<IResult> DeletePermission(IMediator mediator, int permissionId)
    {
        var result = await mediator.Send(new DeletePermissionCommand { PermissionId = permissionId });
        return result.ToHttpResult();
    }

    private static async Task<IResult> AssignPermissionToRole(IMediator mediator, [FromBody] AssignPermissionToRoleCommand command)
    {
        var result = await mediator.Send(command);
        return result.ToHttpResult();
    }

    private static async Task<IResult> RemovePermissionFromRole(IMediator mediator, [FromBody] RemovePermissionFromRoleCommand command)
    {
        var result = await mediator.Send(command);
        return result.ToHttpResult();
    }
}

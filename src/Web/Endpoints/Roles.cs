using SAPFIAI.Application.Common.Models;
using SAPFIAI.Application.Roles.Commands.CreateRole;
using SAPFIAI.Application.Roles.Commands.UpdateRole;
using SAPFIAI.Application.Roles.Commands.DeleteRole;
using SAPFIAI.Application.Roles.Commands.AssignRoleToUser;
using SAPFIAI.Application.Roles.Commands.RemoveRoleFromUser;
using SAPFIAI.Application.Roles.Queries.GetRoles;
using SAPFIAI.Application.Roles.Queries.GetRoleById;
using SAPFIAI.Application.Roles.Queries.GetUserRoles;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SAPFIAI.Web.Infrastructure;

namespace SAPFIAI.Web.Endpoints;

public class Roles : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var group = app.MapGroup(this)
            .WithName("Roles")
            .WithOpenApi()
            .RequireAuthorization("RequireAdministrator");

        group.MapGet("/", GetRoles)
            .WithName("GetRoles")
            .Produces<List<RoleDto>>(StatusCodes.Status200OK);

        group.MapGet("/{roleId}", GetRoleById)
            .WithName("GetRoleById")
            .Produces<RoleDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/user/{userId}", GetUserRoles)
            .WithName("GetUserRoles")
            .Produces<List<string>>(StatusCodes.Status200OK);

        group.MapPost("/", CreateRole)
            .WithName("CreateRole")
            .Produces<string>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPut("/{roleId}", UpdateRole)
            .WithName("UpdateRole")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{roleId}", DeleteRole)
            .WithName("DeleteRole")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/assign", AssignRoleToUser)
            .WithName("AssignRoleToUser")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPost("/remove", RemoveRoleFromUser)
            .WithName("RemoveRoleFromUser")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> GetRoles(IMediator mediator)
    {
        var roles = await mediator.Send(new GetRolesQuery());
        return Results.Ok(roles);
    }

    private static async Task<IResult> GetRoleById(IMediator mediator, string roleId)
    {
        var role = await mediator.Send(new GetRoleByIdQuery { RoleId = roleId });
        return role != null ? Results.Ok(role) : Results.NotFound();
    }

    private static async Task<IResult> GetUserRoles(IMediator mediator, string userId)
    {
        var roles = await mediator.Send(new GetUserRolesQuery { UserId = userId });
        return Results.Ok(roles);
    }

    private static async Task<IResult> CreateRole(IMediator mediator, [FromBody] CreateRoleCommand command)
    {
        var result = await mediator.Send(command);
        return result.ToCreatedResult(id => $"/api/roles/{id}");
    }

    private static async Task<IResult> UpdateRole(IMediator mediator, string roleId, [FromBody] UpdateRoleCommand command)
    {
        if (roleId != command.RoleId)
            return Results.BadRequest(new { error = "RoleId mismatch" });

        var result = await mediator.Send(command);
        return result.ToHttpResult();
    }

    private static async Task<IResult> DeleteRole(IMediator mediator, string roleId)
    {
        var result = await mediator.Send(new DeleteRoleCommand { RoleId = roleId });
        return result.ToHttpResult();
    }

    private static async Task<IResult> AssignRoleToUser(IMediator mediator, [FromBody] AssignRoleToUserCommand command)
    {
        var result = await mediator.Send(command);
        return result.ToHttpResult();
    }

    private static async Task<IResult> RemoveRoleFromUser(IMediator mediator, [FromBody] RemoveRoleFromUserCommand command)
    {
        var result = await mediator.Send(command);
        return result.ToHttpResult();
    }
}

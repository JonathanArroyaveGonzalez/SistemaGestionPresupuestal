using Microsoft.AspNetCore.Mvc;
using SAPFIAI.Application.Users.Commands.CreateManagedUser;
using SAPFIAI.Domain.Constants;

namespace SAPFIAI.Web.Endpoints;

public class ManageUsers : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var group = app.MapGroup(this)
            .WithName("ManageUsers")
            .RequireAuthorization();

        group.MapPost("/", CreateUser)
            .WithName("CreateManagedUser")
            .WithSummary("Crear usuario administrado")
            .WithDescription("Crea un usuario local en Identity, lo sincroniza con Permit.io y le asigna el rol inicial indicado.")
            .Produces<CreateManagedUserResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .RequirePermit(PermitConstants.Resources.ManageUsers, PermitConstants.Actions.Create);
    }

    private static async Task<CreateManagedUserResponse> CreateUser(
        [FromBody] CreateManagedUserCommand command,
        IMediator mediator,
        HttpContext httpContext)
    {
        command = command with
        {
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = httpContext.Request.Headers.UserAgent.ToString()
        };

        return await mediator.Send(command);
    }
}
using SAPFIAI.Application.Common.Models;
using SAPFIAI.Application.Security.Commands.BlockIp;
using SAPFIAI.Application.Security.Commands.UnblockIp;
using SAPFIAI.Application.Security.Commands.UnlockAccount;
using SAPFIAI.Application.Security.Queries.GetBlockedIps;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SAPFIAI.Web.Infrastructure;

namespace SAPFIAI.Web.Endpoints;

public class Security : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var group = app.MapGroup(this)
            .WithName("Security")
            .WithOpenApi()
            .RequireAuthorization("CanPurge");

        group.MapGet("/blocked-ips", GetBlockedIps)
            .WithName("GetBlockedIps")
            .Produces<IEnumerable<IpBlackListDto>>(StatusCodes.Status200OK)
            .WithOpenApi();

        group.MapPost("/block-ip", BlockIp)
            .WithName("BlockIp")
            .Produces<Result>(StatusCodes.Status200OK)
            .WithOpenApi();

        group.MapPost("/unblock-ip", UnblockIp)
            .WithName("UnblockIp")
            .Produces<Result>(StatusCodes.Status200OK)
            .WithOpenApi();

        group.MapPost("/unlock-account", UnlockAccount)
            .WithName("UnlockAccount")
            .Produces<Result>(StatusCodes.Status200OK)
            .WithOpenApi();
    }

    private static async Task<IEnumerable<IpBlackListDto>> GetBlockedIps(
        IMediator mediator,
        [FromQuery] bool activeOnly = true)
    {
        var query = new GetBlockedIpsQuery { ActiveOnly = activeOnly };
        return await mediator.Send(query);
    }

    private static async Task<IResult> BlockIp(
        [FromBody] BlockIpCommand command,
        IMediator mediator)
    {
        var result = await mediator.Send(command);
        return result.ToHttpResult();
    }

    private static async Task<IResult> UnblockIp(
        [FromBody] UnblockIpCommand command,
        IMediator mediator)
    {
        var result = await mediator.Send(command);
        return result.ToHttpResult();
    }

    private static async Task<IResult> UnlockAccount(
        [FromBody] UnlockAccountCommand command,
        IMediator mediator)
    {
        var result = await mediator.Send(command);
        return result.ToHttpResult();
    }
}


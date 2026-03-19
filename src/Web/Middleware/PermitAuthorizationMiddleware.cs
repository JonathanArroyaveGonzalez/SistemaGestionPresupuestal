using System.Security.Claims;
using SAPFIAI.Application.Common.Interfaces;
using SAPFIAI.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;

namespace SAPFIAI.Web.Middleware;

public class PermitAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PermitAuthorizationMiddleware> _logger;

    public PermitAuthorizationMiddleware(RequestDelegate next, ILogger<PermitAuthorizationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() != null)
        {
            await _next(context);
            return;
        }

        var permitRequirement = endpoint?.Metadata.GetMetadata<PermitRequirementMetadata>();
        if (permitRequirement == null)
        {
            await _next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Unauthorized", message = "Debes autenticarte para acceder a este recurso." });
            return;
        }

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? context.User.FindFirstValue("sub");

        if (string.IsNullOrEmpty(userId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Unauthorized", message = "No se pudo resolver el usuario autenticado." });
            return;
        }

        var permitService = context.RequestServices.GetRequiredService<IPermitAuthorizationService>();

        var allowed = await permitService.IsAllowedAsync(userId, permitRequirement.Action, permitRequirement.Resource);

        if (!allowed)
        {
            _logger.LogWarning("Permit.io denied: user={UserId} action={Action} resource={Resource} path={Path}",
                userId, permitRequirement.Action, permitRequirement.Resource, context.Request.Path.Value ?? string.Empty);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "Forbidden", message = "No tienes permiso para realizar esta acción." });
            return;
        }

        await _next(context);
    }
}

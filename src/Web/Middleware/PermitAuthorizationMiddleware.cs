using System.Security.Claims;
using SAPFIAI.Application.Common.Interfaces;

namespace SAPFIAI.Web.Middleware;

/// <summary>
/// Middleware que verifica permisos RBAC en Permit.io para cada request autenticado.
/// Mapea el método HTTP y la ruta al par (action, resource) que se consulta en Permit.io.
/// Rutas públicas (AllowAnonymous) no pasan por esta verificación.
/// </summary>
public class PermitAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PermitAuthorizationMiddleware> _logger;

    // Rutas que no requieren verificación de Permit.io (autenticación propia o públicas)
    private static readonly HashSet<string> _bypassPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/authentication/login",
        "/authentication/register",
        "/authentication/refresh-token",
        "/authentication/forgot-password",
        "/authentication/reset-password",
        "/authentication/verify-2fa",
        "/health",
        "/",
    };

    public PermitAuthorizationMiddleware(RequestDelegate next, ILogger<PermitAuthorizationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IPermitAuthorizationService permitService)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Saltar rutas públicas
        if (ShouldBypass(path))
        {
            await _next(context);
            return;
        }

        // Solo verificar usuarios autenticados
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? context.User.FindFirstValue("sub");

        if (string.IsNullOrEmpty(userId))
        {
            await _next(context);
            return;
        }

        var (action, resource) = MapToPermit(context.Request.Method, path);

        if (action == null || resource == null)
        {
            await _next(context);
            return;
        }

        var allowed = await permitService.IsAllowedAsync(userId, action, resource);

        if (!allowed)
        {
            _logger.LogWarning("Permit.io denied: user={UserId} action={Action} resource={Resource} path={Path}",
                userId, action, resource, path);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "Forbidden", message = "No tienes permiso para realizar esta acción." });
            return;
        }

        await _next(context);
    }

    private static bool ShouldBypass(string path)
    {
        foreach (var bypass in _bypassPaths)
        {
            if (path.StartsWith(bypass, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Mapea método HTTP + segmento de ruta al par (action, resource) de Permit.io.
    /// Convención: el primer segmento de ruta tras /api/ es el resource.
    /// El método HTTP determina la action.
    /// </summary>
    private static (string? action, string? resource) MapToPermit(string method, string path)
    {
        // Extraer el primer segmento significativo de la ruta como resource
        var segments = path.Trim('/').Split('/');
        if (segments.Length == 0 || string.IsNullOrEmpty(segments[0]))
            return (null, null);

        var resource = segments[0].ToLowerInvariant();

        var action = method.ToUpperInvariant() switch
        {
            "GET"    => "read",
            "POST"   => "create",
            "PUT"    => "update",
            "PATCH"  => "update",
            "DELETE" => "delete",
            _        => null
        };

        return (action, resource);
    }
}

using SAPFIAI.Application.Common.Interfaces;
using SAPFIAI.Application.Common.Models;
using SAPFIAI.Application.Users.Commands.EnableTwoFactor;
using SAPFIAI.Application.Users.Commands.ForgotPassword;
using SAPFIAI.Application.Users.Commands.Login;
using SAPFIAI.Application.Users.Commands.Logout;
using SAPFIAI.Application.Users.Commands.RefreshToken;
using SAPFIAI.Application.Users.Commands.ResetPassword;
using SAPFIAI.Application.Users.Commands.RevokeToken;
using SAPFIAI.Application.Users.Commands.ValidateTwoFactor;
using SAPFIAI.Application.Users.Queries;
using SAPFIAI.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SAPFIAI.Web.Infrastructure;

namespace SAPFIAI.Web.Endpoints;

public class Authentication : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var group = app.MapGroup(this)
            .WithName("Authentication");

        group.MapPost("/login", Login)
            .WithName("Login")
            .WithSummary("Iniciar sesión")
            .WithDescription("""
                Autentica al usuario con email y contraseña.

                **Flujo sin 2FA:** retorna `token` (JWT) + `refreshToken` listos para usar.
                Cualquier sesión activa anterior queda revocada automáticamente.

                **Flujo con 2FA:** retorna `requires2FA: true` y un `token` temporal sin privilegios.
                Ese token debe enviarse a `POST /verify-2fa` junto con el código OTP recibido por email.

                **Rate limit:** 10 peticiones por minuto por IP. Excederlo retorna `429`.
                """)
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .Produces<LoginResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status429TooManyRequests)
            .RequireRateLimiting("auth")
            .AllowAnonymous();

        group.MapPost("/verify-2fa", VerifyTwoFactor)
            .WithName("Verify2FA")
            .WithSummary("Verificar código 2FA")
            .WithDescription("""
                Valida el código OTP de 6 dígitos enviado por email y completa el login.

                Requiere el `token` temporal retornado por `POST /login` cuando `requires2FA: true`.
                Al verificar correctamente, las sesiones anteriores del usuario quedan revocadas
                y se retorna un nuevo `token` (JWT) + `refreshToken` definitivos.

                **Rate limit:** 10 peticiones por minuto por IP. Excederlo retorna `429`.
                """)
            .Produces<ValidateTwoFactorResponse>(StatusCodes.Status200OK)
            .Produces<ValidateTwoFactorResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status429TooManyRequests)
            .RequireRateLimiting("auth")
            .AllowAnonymous();

        group.MapPost("/forgot-password", ForgotPassword)
            .WithName("ForgotPassword")
            .WithSummary("Olvidé mi contraseña / Restablecer contraseña")
            .WithDescription("""
                Endpoint de dos modos para el flujo de recuperación de contraseña (usuario NO autenticado).

                **Modo 1 — Solicitar enlace** `{ "email": "..." }`
                Busca el usuario por email, y si existe envía un correo con un enlace que contiene
                el `userId` como parámetro. Por seguridad, siempre retorna `200` aunque el email no exista.

                **Modo 2 — Establecer nueva contraseña** `{ "userId": "...", "newPassword": "..." }`
                Cambia la contraseña del usuario identificado por `userId` (obtenido del enlace del correo).
                No envía ningún correo.

                **Rate limit:** 5 peticiones cada 15 minutos por IP. Excederlo retorna `429`.
                """)
            .Produces<Result>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status429TooManyRequests)
            .RequireRateLimiting("forgot-password")
            .AllowAnonymous();

        group.MapPost("/reset-password", ResetPassword)
            .WithName("ResetPassword")
            .WithSummary("Cambiar contraseña (usuario autenticado)")
            .WithDescription("""
                Cambia la contraseña del usuario actualmente autenticado. Requiere JWT válido.

                A diferencia de `POST /forgot-password`, este endpoint es para usuarios que
                **recuerdan su contraseña actual** y desean cambiarla voluntariamente.

                Body requerido:
                - `oldPassword`: contraseña actual (se valida contra la BD).
                - `newPassword`: nueva contraseña (mín. 8 caracteres, mayúscula, minúscula, número y carácter especial).
                - `confirmPassword`: debe coincidir exactamente con `newPassword`.

                El `userId` se extrae automáticamente del JWT — no debe enviarse en el body.
                """)
            .Produces<Result>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();

        group.MapPost("/refresh-token", RefreshToken)
            .WithName("RefreshToken")
            .WithSummary("Renovar token JWT")
            .WithDescription("Genera un nuevo JWT usando un refresh token válido y no revocado.")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .Produces<LoginResponse>(StatusCodes.Status400BadRequest)
            .AllowAnonymous();

        group.MapPost("/logout", Logout)
            .WithName("Logout")
            .WithSummary("Cerrar sesión")
            .WithDescription("Invalida el refresh token activo del usuario autenticado.")
            .Produces<Result>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();

        group.MapPost("/revoke-token", RevokeToken)
            .WithName("RevokeToken")
            .WithSummary("Revocar refresh token")
            .WithDescription("Revoca un refresh token específico. Útil para cerrar sesión en otros dispositivos.")
            .Produces<Result>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();

        group.MapPost("/enable-2fa", EnableTwoFactor)
            .WithName("EnableTwoFactor")
            .WithSummary("Activar / desactivar 2FA")
            .WithDescription("Habilita o deshabilita la autenticación de dos factores para el usuario autenticado. Envía código OTP por email al activar.")
            .Produces<Result>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();

        group.MapGet("/audit-logs", GetAuditLogs)
            .WithName("GetAuditLogs")
            .WithSummary("Obtener logs de auditoría")
            .WithDescription("Retorna logs paginados de todas las acciones del sistema. Soporta filtros por acción, fecha y paginación.")
            .Produces<PagedResult<AuditLogDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization()
            .RequirePermit(PermitConstants.Resources.ManageUsers, PermitConstants.Actions.Read);

        group.MapGet("/audit-logs/user/{userId}", GetUserAuditLogs)
            .WithName("GetUserAuditLogs")
            .WithSummary("Obtener logs de auditoría por usuario")
            .WithDescription("Retorna logs paginados de las acciones de un usuario específico identificado por su `userId`.")
            .Produces<PagedResult<AuditLogDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization()
            .RequirePermit(PermitConstants.Resources.ManageUsers, PermitConstants.Actions.Read);
    }

    private static async Task<IResult> Login(
        [FromBody] LoginCommand command,
        IMediator mediator,
        HttpContext httpContext)
    {
        command = command with
        {
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = httpContext.Request.Headers.UserAgent.ToString()
        };

        var response = await mediator.Send(command);
        return response.Success ? Results.Ok(response) : Results.BadRequest(response);
    }

    private static async Task<IResult> VerifyTwoFactor(
        [FromBody] ValidateTwoFactorCommand command,
        IMediator mediator,
        HttpContext httpContext)
    {
        command = command with
        {
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = httpContext.Request.Headers.UserAgent.ToString()
        };

        var response = await mediator.Send(command);
        return response.Success ? Results.Ok(response) : Results.BadRequest(response);
    }

    private static async Task<IResult> ForgotPassword(
        [FromBody] ForgotPasswordCommand command,
        IMediator mediator,
        HttpContext httpContext)
    {
        command = command with
        {
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = httpContext.Request.Headers.UserAgent.ToString()
        };

        var result = await mediator.Send(command);
        return result.IsSuccess ? Results.Ok(result) : result.ToHttpResult();
    }

    private static async Task<IResult> ResetPassword(
        [FromBody] ResetPasswordCommand command,
        IMediator mediator,
        IUser user,
        HttpContext httpContext)
    {
        command = command with
        {
            UserId = user.Id ?? string.Empty,
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = httpContext.Request.Headers.UserAgent.ToString()
        };

        return (await mediator.Send(command)).ToHttpResult();
    }

    private static async Task<IResult> RefreshToken(
        [FromBody] RefreshTokenCommand command,
        IMediator mediator,
        HttpContext httpContext)
    {
        command = command with
        {
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = httpContext.Request.Headers.UserAgent.ToString()
        };

        var response = await mediator.Send(command);
        return response.Success ? Results.Ok(response) : Results.BadRequest(response);
    }

    private static async Task<IResult> Logout(
        IMediator mediator,
        IUser user,
        HttpContext httpContext)
    {
        var command = new LogoutCommand
        {
            UserId = user.Id ?? string.Empty,
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = httpContext.Request.Headers.UserAgent.ToString()
        };

        return (await mediator.Send(command)).ToHttpResult();
    }

    private static async Task<IResult> RevokeToken(
        [FromBody] RevokeTokenCommand command,
        IMediator mediator,
        HttpContext httpContext)
    {
        command = command with
        {
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = httpContext.Request.Headers.UserAgent.ToString()
        };

        return (await mediator.Send(command)).ToHttpResult();
    }

    private static async Task<IResult> EnableTwoFactor(
        [FromBody] EnableTwoFactorCommand command,
        IMediator mediator,
        IUser user,
        HttpContext httpContext)
    {
        command = command with
        {
            UserId = user.Id ?? string.Empty,
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = httpContext.Request.Headers.UserAgent.ToString()
        };

        return (await mediator.Send(command)).ToHttpResult();
    }

    private static async Task<IResult> GetAuditLogs(
        IMediator mediator,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? action = null)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = new GetAuditLogsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Action = action
        };

        return Results.Ok(await mediator.Send(query));
    }

    private static async Task<IResult> GetUserAuditLogs(
        IMediator mediator,
        [FromRoute] string userId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = new GetAuditLogsQuery
        {
            UserId = userId,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return Results.Ok(await mediator.Send(query));
    }
}

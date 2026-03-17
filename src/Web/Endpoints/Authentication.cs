using SAPFIAI.Application.Common.Interfaces;
using SAPFIAI.Application.Common.Models;
using SAPFIAI.Application.Users.Commands.EnableTwoFactor;
using SAPFIAI.Application.Users.Commands.ForgotPassword;
using SAPFIAI.Application.Users.Commands.Login;
using SAPFIAI.Application.Users.Commands.Logout;
using SAPFIAI.Application.Users.Commands.RefreshToken;
using SAPFIAI.Application.Users.Commands.Register;
using SAPFIAI.Application.Users.Commands.ResetPassword;
using SAPFIAI.Application.Users.Commands.RevokeToken;
using SAPFIAI.Application.Users.Commands.ValidateTwoFactor;
using SAPFIAI.Application.Users.Queries;
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

        group.MapPost("/register", Register)
            .WithName("Register")
            .WithSummary("Registrar usuario")
            .WithDescription("Crea una nueva cuenta de usuario. Requiere email, password y confirmación.")
            .Produces<RegisterResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .AllowAnonymous();

        group.MapPost("/login", Login)
            .WithName("Login")
            .WithSummary("Iniciar sesión")
            .WithDescription("Autentica al usuario y retorna JWT + refresh token. Si tiene 2FA activo, retorna `requires2FA: true` y no incluye token.")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .AllowAnonymous();

        group.MapPost("/verify-2fa", VerifyTwoFactor)
            .WithName("Verify2FA")
            .WithSummary("Verificar código 2FA")
            .WithDescription("Valida el código OTP de 6 dígitos enviado por email. Requiere el token temporal retornado por `/login`.")
            .Produces<ValidateTwoFactorResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .AllowAnonymous();

        group.MapPost("/forgot-password", ForgotPassword)
            .WithName("ForgotPassword")
            .WithSummary("Solicitar restablecimiento de contraseña")
            .WithDescription("Envía un email con el token para restablecer la contraseña.")
            .Produces<Result>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .AllowAnonymous();

        group.MapPost("/reset-password", ResetPassword)
            .WithName("ResetPassword")
            .WithSummary("Restablecer contraseña")
            .WithDescription("Establece una nueva contraseña usando el token recibido por email.")
            .Produces<Result>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .AllowAnonymous();

        group.MapPost("/refresh-token", RefreshToken)
            .WithName("RefreshToken")
            .WithSummary("Renovar token JWT")
            .WithDescription("Genera un nuevo JWT usando un refresh token válido y no revocado.")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
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
            .Produces<IEnumerable<AuditLogDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();

        group.MapGet("/audit-logs/user/{userId}", GetUserAuditLogs)
            .WithName("GetUserAuditLogs")
            .WithSummary("Obtener logs de auditoría por usuario")
            .WithDescription("Retorna logs paginados de las acciones de un usuario específico identificado por su `userId`.")
            .Produces<IEnumerable<AuditLogDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();
    }

    private static async Task<RegisterResponse> Register(
        [FromBody] RegisterCommand command,
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

    private static async Task<LoginResponse> Login(
        [FromBody] LoginCommand command,
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

    private static async Task<ValidateTwoFactorResponse> VerifyTwoFactor(
        [FromBody] ValidateTwoFactorCommand command,
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

    private static async Task<Result> ForgotPassword(
        [FromBody] ForgotPasswordCommand command,
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

    private static async Task<Result> ResetPassword(
        [FromBody] ResetPasswordCommand command,
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

    private static async Task<LoginResponse> RefreshToken(
        [FromBody] RefreshTokenCommand command,
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

    private static async Task<Result> Logout(
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

        return await mediator.Send(command);
    }

    private static async Task<Result> RevokeToken(
        [FromBody] RevokeTokenCommand command,
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

    private static async Task<Result> EnableTwoFactor(
        [FromBody] EnableTwoFactorCommand command,
        IMediator mediator,
        IUser user)
    {
        command = command with
        {
            UserId = user.Id ?? string.Empty
        };

        return await mediator.Send(command);
    }

    private static async Task<IEnumerable<AuditLogDto>> GetAuditLogs(
        IMediator mediator,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? action = null)
    {
        var query = new GetAuditLogsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Action = action
        };

        return await mediator.Send(query);
    }

    private static async Task<IEnumerable<AuditLogDto>> GetUserAuditLogs(
        IMediator mediator,
        [FromRoute] string userId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetAuditLogsQuery
        {
            UserId = userId,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return await mediator.Send(query);
    }
}

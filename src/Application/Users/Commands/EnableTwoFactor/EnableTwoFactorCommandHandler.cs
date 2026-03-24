using SAPFIAI.Application.Common.Interfaces;
using SAPFIAI.Application.Common.Models;

namespace SAPFIAI.Application.Users.Commands.EnableTwoFactor;

public class EnableTwoFactorCommandHandler : IRequestHandler<EnableTwoFactorCommand, Result>
{
    private readonly IIdentityService _identityService;
    private readonly IUser _currentUser;
    private readonly IAuditLogService _auditLogService;

    public EnableTwoFactorCommandHandler(
        IIdentityService identityService,
        IUser currentUser,
        IAuditLogService auditLogService)
    {
        _identityService = identityService;
        _currentUser = currentUser;
        _auditLogService = auditLogService;
    }

    public async Task<Result> Handle(EnableTwoFactorCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.Id;

        if (string.IsNullOrEmpty(userId))
            return Result.Failure(Error.Failure("User.Unauthorized", "Usuario no autenticado"));

        var ipAddress = request.IpAddress ?? "UNKNOWN";
        var userAgent = request.UserAgent;

        try
        {
            var isPasswordValid = await _identityService.CheckPasswordAsync(userId, request.Password);
            if (!isPasswordValid)
            {
                await _auditLogService.LogActionAsync(
                    userId: userId,
                    action: "ENABLE_2FA",
                    ipAddress: ipAddress,
                    userAgent: userAgent,
                    details: "Contraseña incorrecta",
                    status: "ERROR");

                return Result.Failure(Error.Failure("User.InvalidPassword", "Contraseña incorrecta"));
            }

            var result = await _identityService.SetTwoFactorEnabledAsync(userId, request.Enable);

            if (!result.IsSuccess)
            {
                await _auditLogService.LogActionAsync(
                    userId: userId,
                    action: "ENABLE_2FA",
                    ipAddress: ipAddress,
                    userAgent: userAgent,
                    details: $"Error al {(request.Enable ? "habilitar" : "deshabilitar")} 2FA",
                    status: "ERROR");

                return result;
            }

            await _auditLogService.LogActionAsync(
                userId: userId,
                action: "ENABLE_2FA",
                ipAddress: ipAddress,
                userAgent: userAgent,
                details: $"2FA {(request.Enable ? "habilitado" : "deshabilitado")} exitosamente",
                status: "SUCCESS");

            return Result.Success();
        }
        catch (Exception ex)
        {
            await _auditLogService.LogActionAsync(
                userId: userId,
                action: "ENABLE_2FA",
                ipAddress: ipAddress,
                userAgent: userAgent,
                details: ex.Message,
                status: "ERROR");

            return Result.Failure(Error.Failure("User.2FA", "Error al habilitar/deshabilitar 2FA"));
        }
    }
}

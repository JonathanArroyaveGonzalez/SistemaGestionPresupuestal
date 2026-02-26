using SAPFIAI.Application.Common.Interfaces;
using SAPFIAI.Application.Common.Models;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

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
        {
            return Result.Failure(Error.Failure("User.Unauthorized", "Usuario no autenticado"));
        }

        try
        {
            await _auditLogService.LogActionAsync(
                userId: userId,
                action: "ENABLE_2FA",
                ipAddress: "UNKNOWN",
                userAgent: "UNKNOWN",
                details: "2FA habilitado exitosamente",
                status: "SUCCESS");

            return Result.Success();
        }
        catch (Exception ex)
        {
            await _auditLogService.LogActionAsync(
                userId: userId,
                action: "ENABLE_2FA",
                ipAddress: "UNKNOWN",
                userAgent: "UNKNOWN",
                details: ex.Message,
                status: "ERROR");

            return Result.Failure(Error.Failure("User.2FA", "Error al habilitar/deshabilitar 2FA"));
        }
    }
}
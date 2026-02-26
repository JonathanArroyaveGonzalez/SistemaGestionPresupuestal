using SAPFIAI.Application.Common.Interfaces;
using SAPFIAI.Application.Common.Models;
using MediatR;

namespace SAPFIAI.Application.Users.Commands.ResetPassword;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
{
    private readonly IIdentityService _identityService;
    private readonly IAuditLogService _auditLogService;

    public ResetPasswordCommandHandler(
        IIdentityService identityService,
        IAuditLogService auditLogService)
    {
        _identityService = identityService;
        _auditLogService = auditLogService;
    }

    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var result = await _identityService.ResetPasswordAsync(
            request.Email,
            request.Token,
            request.NewPassword);

        if (!result.IsSuccess)
        {
            await _auditLogService.LogActionAsync(
                userId: request.Email,
                action: "PASSWORD_RESET_FAILED",
                ipAddress: request.IpAddress ?? "UNKNOWN",
                userAgent: request.UserAgent,
                details: result.Error.Description,
                status: "FAILED");

            return result;
        }

        await _auditLogService.LogActionAsync(
            userId: request.Email,
            action: "PASSWORD_RESET_SUCCESS",
            ipAddress: request.IpAddress ?? "UNKNOWN",
            userAgent: request.UserAgent,
            details: "Contraseña restablecida exitosamente",
            status: "SUCCESS");

        return Result.Success();
    }
}
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
        var result = await _identityService.ChangePasswordAsync(
            request.UserId!,
            request.OldPassword,
            request.NewPassword);

        await _auditLogService.LogActionAsync(
            userId: request.UserId!,
            action: result.IsSuccess ? "PASSWORD_CHANGE_SUCCESS" : "PASSWORD_CHANGE_FAILED",
            ipAddress: request.IpAddress ?? "UNKNOWN",
            userAgent: request.UserAgent,
            details: result.IsSuccess ? "Contraseña cambiada exitosamente" : result.Error.Description,
            status: result.IsSuccess ? "SUCCESS" : "FAILED");

        return result;
    }
}

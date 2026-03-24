using SAPFIAI.Application.Common.Interfaces;
using SAPFIAI.Application.Common.Models;
using MediatR;

namespace SAPFIAI.Application.Users.Commands.ForgotPassword;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result>
{
    private readonly IIdentityService _identityService;
    private readonly IEmailService _emailService;
    private readonly IAuditLogService _auditLogService;

    public ForgotPasswordCommandHandler(
        IIdentityService identityService,
        IEmailService emailService,
        IAuditLogService auditLogService)
    {
        _identityService = identityService;
        _emailService = emailService;
        _auditLogService = auditLogService;
    }

    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        // Modo 2: userId + newPassword → cambiar contraseña
        if (!string.IsNullOrEmpty(request.UserId))
        {
            var result = await _identityService.ResetPasswordByUserIdAsync(request.UserId, request.NewPassword!);

            await _auditLogService.LogActionAsync(
                userId: request.UserId,
                action: result.IsSuccess ? "PASSWORD_RESET_SUCCESS" : "PASSWORD_RESET_FAILED",
                ipAddress: request.IpAddress ?? "UNKNOWN",
                userAgent: request.UserAgent,
                details: result.IsSuccess ? "Contraseña restablecida exitosamente" : result.Error.Description,
                status: result.IsSuccess ? "SUCCESS" : "FAILED");

            return result;
        }

        // Modo 1: solo email → obtener userId y enviar correo
        var (found, userId) = await _identityService.GetUserIdByEmailAsync(request.Email!);

        if (!found || string.IsNullOrEmpty(userId))
            return Result.Success(); // No revelar si el email existe

        var emailSent = await _emailService.SendPasswordResetAsync(request.Email!, request.Email!, userId);

        await _auditLogService.LogActionAsync(
            userId: userId,
            action: "PASSWORD_RESET_REQUESTED",
            ipAddress: request.IpAddress ?? "UNKNOWN",
            userAgent: request.UserAgent,
            details: emailSent ? "Email de reset enviado" : "Error al enviar email",
            status: emailSent ? "SUCCESS" : "WARNING");

        return Result.Success();
    }
}

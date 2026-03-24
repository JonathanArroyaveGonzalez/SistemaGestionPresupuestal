using SAPFIAI.Application.Common.Interfaces;
using SAPFIAI.Application.Common.Models;
using MediatR;
using Microsoft.Extensions.Caching.Memory;

namespace SAPFIAI.Application.Users.Commands.ForgotPassword;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result>
{
    private readonly IIdentityService _identityService;
    private readonly IEmailService _emailService;
    private readonly IAuditLogService _auditLogService;
    private readonly IMemoryCache _cache;
    private const string CooldownKeyPrefix = "ForgotPassword:Cooldown:";
    private static readonly TimeSpan CooldownPeriod = TimeSpan.FromMinutes(2);

    public ForgotPasswordCommandHandler(
        IIdentityService identityService,
        IEmailService emailService,
        IAuditLogService auditLogService,
        IMemoryCache cache)
    {
        _identityService = identityService;
        _emailService = emailService;
        _auditLogService = auditLogService;
        _cache = cache;
    }

    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        // ── Paso 2: reset de contraseña con userId ───────────────────────────
        if (!string.IsNullOrEmpty(request.UserId))
            return await HandleResetAsync(request, cancellationToken);

        // ── Paso 1: solicitud por email ──────────────────────────────────────
        return await HandleRequestAsync(request, cancellationToken);
    }

    // Paso 1 — recibe email, envía enlace con userId
    private async Task<Result> HandleRequestAsync(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email!.Trim().ToLowerInvariant();
        var cooldownKey = $"{CooldownKeyPrefix}{email}";

        // Cooldown: evita spam y correos duplicados por doble click / reintentos
        if (_cache.TryGetValue(cooldownKey, out _))
            return Result.Success();

        var (userExists, userId) = await _identityService.GetUserIdByEmailAsync(email);

        // Siempre retornar éxito para no revelar si el email existe
        if (!userExists || string.IsNullOrEmpty(userId))
            return Result.Success();

        // Registrar cooldown ANTES de enviar para bloquear requests concurrentes
        _cache.Set(cooldownKey, true, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CooldownPeriod
        });

        // Enviar el userId en el enlace — el frontend lo incluirá en el paso 2
        var emailSent = await _emailService.SendPasswordResetAsync(email, email, userId);

        await _auditLogService.LogActionAsync(
            userId: userId,
            action: "PASSWORD_RESET_REQUESTED",
            ipAddress: request.IpAddress ?? "UNKNOWN",
            userAgent: request.UserAgent,
            details: emailSent ? "Email de reset enviado" : "Error al enviar email",
            status: emailSent ? "SUCCESS" : "WARNING");

        return Result.Success();
    }

    // Paso 2 — recibe userId + nueva contraseña, resetea directamente
    private async Task<Result> HandleResetAsync(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var result = await _identityService.ResetPasswordByUserIdAsync(
            request.UserId!,
            request.NewPassword!);

        if (!result.IsSuccess)
        {
            await _auditLogService.LogActionAsync(
                userId: request.UserId!,
                action: "PASSWORD_RESET_FAILED",
                ipAddress: request.IpAddress ?? "UNKNOWN",
                userAgent: request.UserAgent,
                details: result.Error.Description,
                status: "FAILED");

            return result;
        }

        await _auditLogService.LogActionAsync(
            userId: request.UserId!,
            action: "PASSWORD_RESET_SUCCESS",
            ipAddress: request.IpAddress ?? "UNKNOWN",
            userAgent: request.UserAgent,
            details: "Contraseña restablecida exitosamente",
            status: "SUCCESS");

        return Result.Success();
    }
}

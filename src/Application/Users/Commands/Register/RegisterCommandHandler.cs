using SAPFIAI.Application.Common.Interfaces;
using MediatR;

namespace SAPFIAI.Application.Users.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResponse>
{
    private readonly IIdentityService _identityService;
    private readonly IEmailService _emailService;
    private readonly IAuditLogService _auditLogService;

    public RegisterCommandHandler(
        IIdentityService identityService,
        IEmailService emailService,
        IAuditLogService auditLogService)
    {
        _identityService = identityService;
        _emailService = emailService;
        _auditLogService = auditLogService;
    }

    public async Task<RegisterResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var (result, userId) = await _identityService.CreateUserAsync(request.Email, request.Password);

        if (!result.IsSuccess)
        {
            await _auditLogService.LogActionAsync(
                userId: request.Email,
                action: "REGISTER_FAILED",
                ipAddress: request.IpAddress ?? "UNKNOWN",
                userAgent: request.UserAgent,
                details: result.Error.Description,
                status: "FAILED");

            return new RegisterResponse
            {
                Success = false,
                Message = "Error al crear el usuario",
                Errors = new[] { result.Error.Description }
            };
        }

        // Registrar auditoría
        await _auditLogService.LogActionAsync(
            userId: userId,
            action: "REGISTER_SUCCESS",
            ipAddress: request.IpAddress ?? "UNKNOWN",
            userAgent: request.UserAgent,
            details: "Usuario registrado: $($request.Email)",
            status: "SUCCESS");

        // Enviar email de confirmación (no bloquea el registro si falla)
        try
        {
            await _emailService.SendRegistrationConfirmationAsync(
                request.Email,
                request.UserName ?? request.Email);
        }
        catch
        {
            // Log pero no falla el registro
        }

        return new RegisterResponse
        {
            Success = true,
            UserId = userId,
            Message = "Usuario registrado exitosamente"
        };
    }
}
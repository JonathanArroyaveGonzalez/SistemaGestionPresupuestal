using SAPFIAI.Application.Common.Interfaces;
using SAPFIAI.Application.Common.Models;
using Microsoft.Extensions.Logging;

namespace SAPFIAI.Application.Users.Commands.CreateManagedUser;

public class CreateManagedUserCommandHandler : IRequestHandler<CreateManagedUserCommand, CreateManagedUserResponse>
{
    private readonly IIdentityService _identityService;
    private readonly IPermitProvisioningService _permitProvisioningService;
    private readonly IEmailService _emailService;
    private readonly IAuditLogService _auditLogService;
    private readonly IUser _currentUser;
    private readonly ILogger<CreateManagedUserCommandHandler> _logger;

    public CreateManagedUserCommandHandler(
        IIdentityService identityService,
        IPermitProvisioningService permitProvisioningService,
        IEmailService emailService,
        IAuditLogService auditLogService,
        IUser currentUser,
        ILogger<CreateManagedUserCommandHandler> logger)
    {
        _identityService = identityService;
        _permitProvisioningService = permitProvisioningService;
        _emailService = emailService;
        _auditLogService = auditLogService;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<CreateManagedUserResponse> Handle(CreateManagedUserCommand request, CancellationToken cancellationToken)
    {
        var actorUserId = _currentUser.Id ?? "UNKNOWN";
        var normalizedRole = request.RoleKey.Trim().ToLowerInvariant();

        var (result, userId) = await _identityService.CreateUserAsync(new CreateIdentityUserRequest
        {
            Email = request.Email,
            Password = request.Password,
            UserName = request.UserName,
            PhoneNumber = request.PhoneNumber
        });

        if (!result.IsSuccess)
        {
            await _auditLogService.LogActionAsync(
                userId: actorUserId,
                action: "MANAGE_USERS_CREATE_FAILED",
                ipAddress: request.IpAddress ?? "UNKNOWN",
                userAgent: request.UserAgent,
                details: $"No se pudo crear el usuario {request.Email}: {result.Error.Description}",
                status: "FAILED",
                resourceId: request.Email,
                resourceType: "user");

            return new CreateManagedUserResponse
            {
                Success = false,
                Message = "Error al crear el usuario",
                Errors = new[] { result.Error.Description }
            };
        }

        try
        {
            await _permitProvisioningService.SyncUserAsync(new PermitUserSyncRequest
            {
                UserId = userId,
                Email = request.Email,
                UserName = request.UserName,
                PhoneNumber = request.PhoneNumber
            }, cancellationToken);

            await _permitProvisioningService.AssignRoleAsync(userId, normalizedRole, cancellationToken);
        }
        catch (Exception ex)
        {
            await RollbackManagedUserCreationAsync(userId, cancellationToken);

            await _auditLogService.LogActionAsync(
                userId: actorUserId,
                action: "MANAGE_USERS_CREATE_FAILED",
                ipAddress: request.IpAddress ?? "UNKNOWN",
                userAgent: request.UserAgent,
                details: $"Error sincronizando el usuario {request.Email} con Permit.io: {ex.Message}",
                status: "FAILED",
                errorMessage: ex.Message,
                resourceId: userId,
                resourceType: "user");

            return new CreateManagedUserResponse
            {
                Success = false,
                Message = "No se pudo sincronizar el usuario con Permit.io",
                Errors = new[] { ex.Message }
            };
        }

        await _auditLogService.LogActionAsync(
            userId: actorUserId,
            action: "MANAGE_USERS_CREATE_SUCCESS",
            ipAddress: request.IpAddress ?? "UNKNOWN",
            userAgent: request.UserAgent,
            details: $"Usuario {request.Email} creado con rol {normalizedRole}",
            status: "SUCCESS",
            resourceId: userId,
            resourceType: "user");

        try
        {
            await _emailService.SendRegistrationConfirmationAsync(
                request.Email,
                request.UserName ?? request.Email);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send registration confirmation email to {Email}", request.Email);
        }

        return new CreateManagedUserResponse
        {
            Success = true,
            UserId = userId,
            RoleKey = normalizedRole,
            Message = "Usuario creado y sincronizado exitosamente"
        };
    }

    private async Task RollbackManagedUserCreationAsync(string userId, CancellationToken cancellationToken)
    {
        try
        {
            await _permitProvisioningService.DeleteUserAsync(userId, cancellationToken);
        }
        catch
        {
        }

        try
        {
            await _identityService.DeleteUserAsync(userId);
        }
        catch
        {
        }
    }
}
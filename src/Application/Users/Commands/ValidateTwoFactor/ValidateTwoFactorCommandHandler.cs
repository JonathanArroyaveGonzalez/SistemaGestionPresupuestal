using SAPFIAI.Application.Common.Interfaces;
using SAPFIAI.Application.Common.Models;
using MediatR;

namespace SAPFIAI.Application.Users.Commands.ValidateTwoFactor;

public class ValidateTwoFactorCommandHandler : IRequestHandler<ValidateTwoFactorCommand, ValidateTwoFactorResponse>
{
    private readonly IAuthenticationOperations _authOperations;
    private readonly ITwoFactorService _twoFactorService;
    private readonly IAuditLogService _auditLogService;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IIdentityService _identityService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IPermissionService _permissionService;

    public ValidateTwoFactorCommandHandler(
        IAuthenticationOperations authOperations,
        ITwoFactorService twoFactorService,
        IAuditLogService auditLogService,
        IJwtTokenGenerator jwtTokenGenerator,
        IIdentityService identityService,
        IRefreshTokenService refreshTokenService,
        IPermissionService permissionService)
    {
        _authOperations = authOperations;
        _twoFactorService = twoFactorService;
        _auditLogService = auditLogService;
        _jwtTokenGenerator = jwtTokenGenerator;
        _identityService = identityService;
        _refreshTokenService = refreshTokenService;
        _permissionService = permissionService;
    }

    public async Task<ValidateTwoFactorResponse> Handle(ValidateTwoFactorCommand request, CancellationToken cancellationToken)
    {
        // Validar que el token existe y requiere verificación 2FA
        if (string.IsNullOrEmpty(request.Token))
        {
            return new ValidateTwoFactorResponse
            {
                Success = false,
                Message = "Token no proporcionado",
                Errors = new[] { "Se requiere el token JWT" }
            };
        }

        // Verificar que el token tiene pendiente la verificación 2FA
        if (!_jwtTokenGenerator.RequiresTwoFactorVerification(request.Token))
        {
            return new ValidateTwoFactorResponse
            {
                Success = false,
                Message = "Token inválido o ya verificado",
                Errors = new[] { "El token no requiere verificación 2FA" }
            };
        }

        // Extraer userId del token
        var userId = _jwtTokenGenerator.GetUserIdFromToken(request.Token);
        if (string.IsNullOrEmpty(userId))
        {
            return new ValidateTwoFactorResponse
            {
                Success = false,
                Message = "Token inválido",
                Errors = new[] { "No se pudo extraer el usuario del token" }
            };
        }

        // Obtener usuario
        var user = await _authOperations.GetUserByIdAsync(userId);
        if (user == null)
        {
            await _auditLogService.LogActionAsync(
                userId: userId,
                action: "2FA_VALIDATION_FAILED",
                ipAddress: request.IpAddress ?? "UNKNOWN",
                userAgent: request.UserAgent,
                details: "Usuario no encontrado",
                status: "FAILED");

            return new ValidateTwoFactorResponse
            {
                Success = false,
                Message = "Usuario no encontrado",
                Errors = new[] { "El usuario asociado al token no existe" }
            };
        }

        // Validar código 2FA
        var isValid = await _twoFactorService.ValidateTwoFactorCodeAsync(userId, request.Code);
        if (!isValid)
        {
            await _auditLogService.LogActionAsync(
                userId: userId,
                action: "2FA_VALIDATION_FAILED",
                ipAddress: request.IpAddress ?? "UNKNOWN",
                userAgent: request.UserAgent,
                details: "Código inválido o expirado",
                status: "FAILED");

            return new ValidateTwoFactorResponse
            {
                Success = false,
                Message = "Código inválido o expirado",
                Errors = new[] { "El código 2FA es incorrecto o ha expirado" }
            };
        }

        // Limpiar código 2FA usado
        await _twoFactorService.ClearTwoFactorCodeAsync(userId);

        // Generar nuevo JWT sin flag de 2FA pendiente
        var userRoles = await _identityService.GetUserRolesAsync(userId);
        var userPermissions = await _permissionService.GetUserPermissionsAsync(userId);
        var newToken = _jwtTokenGenerator.GenerateToken(userId, user.Email ?? string.Empty, userRoles, userPermissions, requiresTwoFactorVerification: false);
        var refreshToken = await _refreshTokenService.GenerateRefreshTokenAsync(userId, request.IpAddress ?? "UNKNOWN");

        // Actualizar datos de login
        await _authOperations.UpdateLastLoginAsync(userId, request.IpAddress);

        await _auditLogService.LogActionAsync(
            userId: userId,
            action: "LOGIN_SUCCESS",
            ipAddress: request.IpAddress ?? "UNKNOWN",
            userAgent: request.UserAgent,
            details: "Login completado con verificación 2FA",
            status: "SUCCESS");

        return new ValidateTwoFactorResponse
        {
            Success = true,
            Token = newToken,
            RefreshToken = refreshToken.Token,
            RefreshTokenExpiry = refreshToken.ExpiryDate,
            User = user,
            Message = "Verificación exitosa"
        };
    }
}

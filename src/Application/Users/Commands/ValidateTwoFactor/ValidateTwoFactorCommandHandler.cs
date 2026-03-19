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
    private readonly IRefreshTokenService _refreshTokenService;

    public ValidateTwoFactorCommandHandler(
        IAuthenticationOperations authOperations,
        ITwoFactorService twoFactorService,
        IAuditLogService auditLogService,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenService refreshTokenService)
    {
        _authOperations = authOperations;
        _twoFactorService = twoFactorService;
        _auditLogService = auditLogService;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenService = refreshTokenService;
    }

    public async Task<ValidateTwoFactorResponse> Handle(ValidateTwoFactorCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Token))
        {
            return new ValidateTwoFactorResponse
            {
                Success = false,
                Message = "Token no proporcionado",
                Errors = new[] { "Se requiere el token JWT" }
            };
        }

        if (!_jwtTokenGenerator.RequiresTwoFactorVerification(request.Token))
        {
            return new ValidateTwoFactorResponse
            {
                Success = false,
                Message = "Token inválido o ya verificado",
                Errors = new[] { "El token no requiere verificación 2FA" }
            };
        }

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

        await _twoFactorService.ClearTwoFactorCodeAsync(userId);

        var newToken = _jwtTokenGenerator.GenerateToken(userId, user.Email ?? string.Empty, requiresTwoFactorVerification: false);
        var refreshToken = await _refreshTokenService.GenerateRefreshTokenAsync(userId, request.IpAddress ?? "UNKNOWN");

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

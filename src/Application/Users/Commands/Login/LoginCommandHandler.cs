using SAPFIAI.Application.Common.Interfaces;
using SAPFIAI.Application.Common.Models;
using MediatR;

namespace SAPFIAI.Application.Users.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IAuthenticationOperations _authOperations;
    private readonly ITwoFactorService _twoFactorService;
    private readonly IAuditLogService _auditLogService;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IAccountLockService _accountLockService;
    private readonly IRefreshTokenService _refreshTokenService;

    public LoginCommandHandler(
        IAuthenticationOperations authOperations,
        ITwoFactorService twoFactorService,
        IAuditLogService auditLogService,
        IJwtTokenGenerator jwtTokenGenerator,
        IAccountLockService accountLockService,
        IRefreshTokenService refreshTokenService)
    {
        _authOperations = authOperations;
        _twoFactorService = twoFactorService;
        _auditLogService = auditLogService;
        _jwtTokenGenerator = jwtTokenGenerator;
        _accountLockService = accountLockService;
        _refreshTokenService = refreshTokenService;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var ipAddress = request.IpAddress ?? "UNKNOWN";

        // 1. Verificar credenciales
        var (isValid, userId, email) = await _authOperations.VerifyCredentialsAsync(request.Email, request.Password);

        if (!isValid || userId == null || email == null)
        {
            await _auditLogService.LogActionAsync(
                userId: request.Email,
                action: "LOGIN_FAILED",
                ipAddress: ipAddress,
                userAgent: request.UserAgent,
                details: "Credenciales inválidas",
                status: "FAILED");

            return new LoginResponse
            {
                Success = false,
                Message = "Email o contraseña inválidos",
                Errors = new[] { "Credenciales inválidas" }
            };
        }

        // 2. Verificar si la cuenta está bloqueada
        var (isLocked, lockoutEnd, _) = await _accountLockService.GetAccountLockStatusAsync(userId);
        if (isLocked)
        {
            var minutesRemaining = lockoutEnd.HasValue
                ? (int)(lockoutEnd.Value - DateTime.UtcNow).TotalMinutes
                : 0;

            return new LoginResponse
            {
                Success = false,
                Message = $"Tu cuenta ha sido bloqueada temporalmente. Intenta nuevamente en {minutesRemaining} minutos.",
                Errors = new[] { "Cuenta bloqueada por intentos fallidos" }
            };
        }

        // 3. Obtener usuario
        var user = await _authOperations.GetUserByIdAsync(userId);
        if (user == null)
        {
            return new LoginResponse
            {
                Success = false,
                Message = "Usuario no encontrado",
                Errors = new[] { "Usuario no encontrado" }
            };
        }

        // 4. Verificar si el usuario tiene 2FA habilitado
        var has2FA = await _twoFactorService.IsTwoFactorEnabledAsync(userId);

        if (has2FA)
        {
            var tempToken = _jwtTokenGenerator.GenerateToken(userId, email, requiresTwoFactorVerification: true);
            var twoFactorSent = await _twoFactorService.GenerateAndSendTwoFactorCodeAsync(userId);

            if (!twoFactorSent)
            {
                await _auditLogService.LogActionAsync(
                    userId: userId,
                    action: "LOGIN_2FA_SEND_FAILED",
                    ipAddress: ipAddress,
                    userAgent: request.UserAgent,
                    details: "Error al enviar código 2FA",
                    status: "FAILED");

                return new LoginResponse
                {
                    Success = false,
                    Message = "Error al enviar código de verificación",
                    Errors = new[] { "No se pudo enviar el código 2FA al correo" }
                };
            }

            await _auditLogService.LogActionAsync(
                userId: userId,
                action: "LOGIN_PENDING_2FA",
                ipAddress: ipAddress,
                userAgent: request.UserAgent,
                details: "Código 2FA enviado, pendiente verificación",
                status: "PENDING");

            return new LoginResponse
            {
                Success = true,
                Token = tempToken,
                User = user,
                Requires2FA = true,
                Message = "Código de verificación enviado a tu correo electrónico"
            };
        }

        // 5. Flujo sin 2FA: login directo
        var token = _jwtTokenGenerator.GenerateToken(userId, email, requiresTwoFactorVerification: false);
        var refreshToken = await _refreshTokenService.GenerateRefreshTokenAsync(userId, ipAddress);

        await _accountLockService.ResetFailedAttemptsAsync(userId);
        await _authOperations.UpdateLastLoginAsync(userId, ipAddress);

        await _auditLogService.LogActionAsync(
            userId: userId,
            action: "LOGIN_SUCCESS",
            ipAddress: ipAddress,
            userAgent: request.UserAgent,
            details: "Login completado",
            status: "SUCCESS");

        return new LoginResponse
        {
            Success = true,
            Token = token,
            RefreshToken = refreshToken.Token,
            RefreshTokenExpiry = refreshToken.ExpiryDate,
            User = user,
            Requires2FA = false,
            Message = "Login exitoso"
        };
    }
}

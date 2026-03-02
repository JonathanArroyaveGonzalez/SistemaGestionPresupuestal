using SAPFIAI.Application.Common.Interfaces;
using SAPFIAI.Application.Common.Models;
using SAPFIAI.Domain.Enums;
using MediatR;

namespace SAPFIAI.Application.Users.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IAuthenticationOperations _authOperations;
    private readonly ITwoFactorService _twoFactorService;
    private readonly IAuditLogService _auditLogService;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IIdentityService _identityService;
    private readonly IIpBlackListService _ipBlackListService;
    private readonly ILoginAttemptService _loginAttemptService;
    private readonly IAccountLockService _accountLockService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IPermissionService _permissionService;

    public LoginCommandHandler(
        IAuthenticationOperations authOperations,
        ITwoFactorService twoFactorService,
        IAuditLogService auditLogService,
        IJwtTokenGenerator jwtTokenGenerator,
        IIdentityService identityService,
        IIpBlackListService ipBlackListService,
        ILoginAttemptService loginAttemptService,
        IAccountLockService accountLockService,
        IRefreshTokenService refreshTokenService,
        IPermissionService permissionService)
    {
        _authOperations = authOperations;
        _twoFactorService = twoFactorService;
        _auditLogService = auditLogService;
        _jwtTokenGenerator = jwtTokenGenerator;
        _identityService = identityService;
        _ipBlackListService = ipBlackListService;
        _loginAttemptService = loginAttemptService;
        _accountLockService = accountLockService;
        _refreshTokenService = refreshTokenService;
        _permissionService = permissionService;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var ipAddress = request.IpAddress ?? "UNKNOWN";

        // 1. Verificar si la IP está bloqueada
        var isIpBlocked = await _ipBlackListService.IsIpBlockedAsync(ipAddress);
        if (isIpBlocked)
        {
            await _loginAttemptService.RecordAttemptAsync(
                request.Email,
                ipAddress,
                false,
                "IP bloqueada",
                LoginFailureReason.IpBlocked,
                request.UserAgent);

            return new LoginResponse
            {
                Success = false,
                Message = "Acceso denegado. Tu IP ha sido bloqueada.",
                Errors = new[] { "IP bloqueada por razones de seguridad" }
            };
        }

        // 2. Verificar rate limiting por IP
        var recentAttemptsByIp = await _loginAttemptService.GetRecentAttemptsByIpAsync(ipAddress, 15);
        if (recentAttemptsByIp >= 5)
        {
            await _loginAttemptService.RecordAttemptAsync(
                request.Email,
                ipAddress,
                false,
                "Demasiados intentos desde esta IP",
                LoginFailureReason.IpBlocked,
                request.UserAgent);

            return new LoginResponse
            {
                Success = false,
                Message = "Demasiados intentos de login. Intenta de nuevo en 15 minutos.",
                Errors = new[] { "Rate limit excedido" }
            };
        }

        // 3. Verificar credenciales
        var (isValid, userId, email) = await _authOperations.VerifyCredentialsAsync(request.Email, request.Password);

        if (!isValid || userId == null || email == null)
        {
            // Registrar intento fallido
            await _loginAttemptService.RecordAttemptAsync(
                request.Email,
                ipAddress,
                false,
                "Credenciales inválidas",
                LoginFailureReason.InvalidCredentials,
                request.UserAgent);

            await _auditLogService.LogActionAsync(
                userId: request.Email,
                action: "LOGIN_FAILED",
                ipAddress: ipAddress,
                userAgent: request.UserAgent,
                details: "Credenciales inválidas",
                status: "FAILED");

            // Verificar si se debe bloquear la IP
            if (await _loginAttemptService.ShouldBlockIpAsync(ipAddress))
            {
                await _ipBlackListService.BlockIpAsync(
                    ipAddress,
                    "Bloqueado automáticamente por demasiados intentos fallidos",
                    BlackListReason.TooManyAttempts,
                    "SYSTEM",
                    DateTime.UtcNow.AddHours(24));
            }

            return new LoginResponse
            {
                Success = false,
                Message = "Email o contraseña inválidos",
                Errors = new[] { "Credenciales inválidas" }
            };
        }

        // 4. Verificar si la cuenta está bloqueada
        var (isLocked, lockoutEnd, failedAttempts) = await _accountLockService.GetAccountLockStatusAsync(userId);
        if (isLocked)
        {
            await _loginAttemptService.RecordAttemptAsync(
                request.Email,
                ipAddress,
                false,
                "Cuenta bloqueada",
                LoginFailureReason.AccountLocked,
                request.UserAgent);

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

        // 5. Obtener usuario y roles
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

        var userRoles = await _identityService.GetUserRolesAsync(userId);
        var userPermissions = await _permissionService.GetUserPermissionsAsync(userId);

        // 6. Verificar si el usuario tiene 2FA habilitado
        var has2FA = await _twoFactorService.IsTwoFactorEnabledAsync(userId);

        if (has2FA)
        {
            // Flujo con 2FA: generar token temporal y enviar código
            var tempToken = _jwtTokenGenerator.GenerateToken(userId, email, userRoles, userPermissions, requiresTwoFactorVerification: true);

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

            await _loginAttemptService.RecordAttemptAsync(
                request.Email,
                ipAddress,
                true,
                "Pendiente verificación 2FA",
                null,
                request.UserAgent);

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

        // 7. Flujo sin 2FA: login directo con token final + refresh token
        var token = _jwtTokenGenerator.GenerateToken(userId, email, userRoles, userPermissions, requiresTwoFactorVerification: false);
        var refreshToken = await _refreshTokenService.GenerateRefreshTokenAsync(userId, ipAddress);

        // 8. Resetear contadores de intentos fallidos
        await _accountLockService.ResetFailedAttemptsAsync(userId);

        // 9. Actualizar último login
        await _authOperations.UpdateLastLoginAsync(userId, ipAddress);

        // 10. Registrar intento exitoso
        await _loginAttemptService.RecordAttemptAsync(
            request.Email,
            ipAddress,
            true,
            null,
            null,
            request.UserAgent);

        await _auditLogService.LogActionAsync(
            userId: userId,
            action: "LOGIN_SUCCESS",
            ipAddress: ipAddress,
            userAgent: request.UserAgent,
            details: "Login completado sin 2FA",
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

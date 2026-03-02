using SAPFIAI.Application.Common.Interfaces;
using SAPFIAI.Application.Users.Commands.Login;

namespace SAPFIAI.Application.Users.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, LoginResponse>
{
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IIpBlackListService _ipBlackListService;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IIdentityService _identityService;
    private readonly IAuthenticationOperations _authOperations;
    private readonly IAuditLogService _auditLogService;
    private readonly IPermissionService _permissionService;

    public RefreshTokenCommandHandler(
        IRefreshTokenService refreshTokenService,
        IIpBlackListService ipBlackListService,
        IJwtTokenGenerator jwtTokenGenerator,
        IIdentityService identityService,
        IAuthenticationOperations authOperations,
        IAuditLogService auditLogService,
        IPermissionService permissionService)
    {
        _refreshTokenService = refreshTokenService;
        _ipBlackListService = ipBlackListService;
        _jwtTokenGenerator = jwtTokenGenerator;
        _identityService = identityService;
        _authOperations = authOperations;
        _auditLogService = auditLogService;
        _permissionService = permissionService;
    }

    public async Task<LoginResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var ipAddress = request.IpAddress ?? "UNKNOWN";

        // 1. Verificar que la IP no est� bloqueada
        var isIpBlocked = await _ipBlackListService.IsIpBlockedAsync(ipAddress);
        if (isIpBlocked)
        {
            await _auditLogService.LogActionAsync(
                userId: "UNKNOWN",
                action: "REFRESH_TOKEN_BLOCKED_IP",
                ipAddress: ipAddress,
                userAgent: request.UserAgent,
                details: "Intento de refresh token desde IP bloqueada",
                status: "BLOCKED");

            return new LoginResponse
            {
                Success = false,
                Message = "Acceso denegado. Tu IP ha sido bloqueada.",
                Errors = new[] { "IP bloqueada por razones de seguridad" }
            };
        }

        // 2. Validar el refresh token
        var (isValid, userId, email) = await _refreshTokenService.ValidateRefreshTokenAsync(
            request.RefreshToken, 
            ipAddress);

        if (!isValid || userId == null || email == null)
        {
            await _auditLogService.LogActionAsync(
                userId: "UNKNOWN",
                action: "REFRESH_TOKEN_INVALID",
                ipAddress: ipAddress,
                userAgent: request.UserAgent,
                details: "Refresh token inv�lido o expirado",
                status: "FAILED");

            return new LoginResponse
            {
                Success = false,
                Message = "Token de actualizaci�n inv�lido o expirado",
                Errors = new[] { "Por favor, inicia sesi�n nuevamente" }
            };
        }

        // 3. Obtener roles y permisos del usuario
        var userRoles = await _identityService.GetUserRolesAsync(userId);
        var userPermissions = await _permissionService.GetUserPermissionsAsync(userId);

        // 4. Generar nuevo access token
        var newAccessToken = _jwtTokenGenerator.GenerateToken(userId, email, userRoles, userPermissions);

        // 5. Generar nuevo refresh token y revocar el anterior
        var newRefreshToken = await _refreshTokenService.GenerateRefreshTokenAsync(userId, ipAddress);
        await _refreshTokenService.RevokeRefreshTokenAsync(
            request.RefreshToken, 
            ipAddress, 
            "Replaced by new token");

        // 6. Obtener informaci�n del usuario
        var user = await _authOperations.GetUserByIdAsync(userId);

        // 7. Auditar la acci�n
        await _auditLogService.LogActionAsync(
            userId: userId,
            action: "REFRESH_TOKEN_SUCCESS",
            ipAddress: ipAddress,
            userAgent: request.UserAgent,
            details: "Token actualizado exitosamente",
            status: "SUCCESS");

        return new LoginResponse
        {
            Success = true,
            Token = newAccessToken,
            RefreshToken = newRefreshToken.Token,
            RefreshTokenExpiry = newRefreshToken.ExpiryDate,
            User = user,
            Message = "Token actualizado exitosamente"
        };
    }
}

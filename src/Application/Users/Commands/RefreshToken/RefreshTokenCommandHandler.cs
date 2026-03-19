using SAPFIAI.Application.Common.Interfaces;
using SAPFIAI.Application.Users.Commands.Login;

namespace SAPFIAI.Application.Users.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, LoginResponse>
{
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IAuthenticationOperations _authOperations;
    private readonly IAuditLogService _auditLogService;

    public RefreshTokenCommandHandler(
        IRefreshTokenService refreshTokenService,
        IJwtTokenGenerator jwtTokenGenerator,
        IAuthenticationOperations authOperations,
        IAuditLogService auditLogService)
    {
        _refreshTokenService = refreshTokenService;
        _jwtTokenGenerator = jwtTokenGenerator;
        _authOperations = authOperations;
        _auditLogService = auditLogService;
    }

    public async Task<LoginResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var ipAddress = request.IpAddress ?? "UNKNOWN";

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
                details: "Refresh token inválido o expirado",
                status: "FAILED");

            return new LoginResponse
            {
                Success = false,
                Message = "Token de actualización inválido o expirado",
                Errors = new[] { "Por favor, inicia sesión nuevamente" }
            };
        }

        var newAccessToken = _jwtTokenGenerator.GenerateToken(userId, email);

        var newRefreshToken = await _refreshTokenService.GenerateRefreshTokenAsync(userId, ipAddress);
        await _refreshTokenService.RevokeRefreshTokenAsync(request.RefreshToken, ipAddress, "Replaced by new token");

        var user = await _authOperations.GetUserByIdAsync(userId);

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

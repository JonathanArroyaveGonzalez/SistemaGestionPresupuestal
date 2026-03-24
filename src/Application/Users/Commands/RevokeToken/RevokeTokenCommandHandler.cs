using SAPFIAI.Application.Common.Interfaces;
using SAPFIAI.Application.Common.Models;

namespace SAPFIAI.Application.Users.Commands.RevokeToken;

public class RevokeTokenCommandHandler : IRequestHandler<RevokeTokenCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditLogService _auditLogService;

    public RevokeTokenCommandHandler(
        IApplicationDbContext context,
        IAuditLogService auditLogService)
    {
        _context = context;
        _auditLogService = auditLogService;
    }

    public async Task<Result> Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == request.RefreshToken, cancellationToken);

        if (refreshToken == null || !refreshToken.IsActive)
            return Result.Failure(Error.NotFound("Token.Invalid", "Token inválido o ya revocado"));

        refreshToken.Revoke(request.IpAddress ?? "UNKNOWN", "Revocado por el usuario");

        _context.RefreshTokens.Update(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogActionAsync(
            userId: refreshToken.UserId.ToString(),
            action: "TOKEN_REVOKED",
            ipAddress: request.IpAddress ?? "UNKNOWN",
            userAgent: request.UserAgent,
            details: "Token de refresco revocado exitosamente",
            status: "SUCCESS");

        return Result.Success();
    }
}

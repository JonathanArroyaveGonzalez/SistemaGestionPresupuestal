using SAPFIAI.Application.Common.Interfaces;
using SAPFIAI.Application.Common.Models;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

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
        var refreshToken = _context.RefreshTokens.SingleOrDefault(x => x.Token == request.RefreshToken);

        if (refreshToken == null || !refreshToken.IsActive)
        {
            return Result.Failure(Error.NotFound("Token.Invalid", "Token inválido o ya revocado"));
        }

        // Revocar el token
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
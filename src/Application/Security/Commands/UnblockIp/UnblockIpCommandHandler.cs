using SAPFIAI.Application.Common.Interfaces;
using SAPFIAI.Application.Common.Models;

namespace SAPFIAI.Application.Security.Commands.UnblockIp;

public class UnblockIpCommandHandler : IRequestHandler<UnblockIpCommand, Result>
{
    private readonly IIpBlackListService _ipBlackListService;
    private readonly IAuditLogService _auditLogService;
    private readonly IUser _user;

    public UnblockIpCommandHandler(
        IIpBlackListService ipBlackListService,
        IAuditLogService auditLogService,
        IUser user)
    {
        _ipBlackListService = ipBlackListService;
        _auditLogService = auditLogService;
        _user = user;
    }

    public async Task<Result> Handle(UnblockIpCommand request, CancellationToken cancellationToken)
    {
        var unblockedBy = _user.Id ?? "SYSTEM";

        var wasUnblocked = await _ipBlackListService.UnblockIpAsync(
            request.IpAddress,
            unblockedBy);

        if (!wasUnblocked)
        {
            return Result.Failure(Error.NotFound("Security.IpNotBlocked", "IP no encontrada en la lista de bloqueo"));
        }

        await _auditLogService.LogActionAsync(
            userId: unblockedBy,
            action: "IP_UNBLOCKED",
            ipAddress: "SYSTEM",
            userAgent: null,
            details: $"IP {request.IpAddress} desbloqueada",
            status: "SUCCESS");

        return Result.Success();
    }
}

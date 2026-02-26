using SAPFIAI.Application.Common.Interfaces;
using SAPFIAI.Application.Common.Models;

namespace SAPFIAI.Application.Security.Commands.UnlockAccount;

public class UnlockAccountCommandHandler : IRequestHandler<UnlockAccountCommand, Result>
{
    private readonly IAccountLockService _accountLockService;
    private readonly IAuditLogService _auditLogService;
    private readonly IUser _user;

    public UnlockAccountCommandHandler(
        IAccountLockService accountLockService,
        IAuditLogService auditLogService,
        IUser user)
    {
        _accountLockService = accountLockService;
        _auditLogService = auditLogService;
        _user = user;
    }

    public async Task<Result> Handle(UnlockAccountCommand request, CancellationToken cancellationToken)
    {
        var wasUnlocked = await _accountLockService.UnlockAccountAsync(request.UserId);

        if (!wasUnlocked)
        {
            return Result.Failure(Error.Failure("Security.UnlockFailed", "No se pudo desbloquear la cuenta"));
        }

        await _auditLogService.LogActionAsync(
            userId: _user.Id ?? "SYSTEM",
            action: "ACCOUNT_UNLOCKED",
            ipAddress: "SYSTEM",
            userAgent: null,
            details: $"Cuenta {request.UserId} desbloqueada manualmente",
            status: "SUCCESS");

        return Result.Success();
    }
}

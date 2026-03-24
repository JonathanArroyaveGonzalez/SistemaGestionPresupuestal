using SAPFIAI.Application.Common.Models;

namespace SAPFIAI.Application.Common.Interfaces;

public interface IAuditLogService
{
    Task LogActionAsync(
        string userId,
        string action,
        string? ipAddress = null,
        string? userAgent = null,
        string? details = null,
        string status = "SUCCESS",
        string? errorMessage = null,
        string? resourceId = null,
        string? resourceType = null);

    Task LogLoginAsync(string userId, string ipAddress, string? userAgent = null);

    Task LogFailedLoginAsync(string userId, string ipAddress, string? userAgent = null, string? error = null);
}

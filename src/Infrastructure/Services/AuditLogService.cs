using SAPFIAI.Application.Common.Interfaces;
using SAPFIAI.Application.Common.Models;
using SAPFIAI.Domain.Entities;
using SAPFIAI.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace SAPFIAI.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(ApplicationDbContext dbContext, ILogger<AuditLogService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task LogActionAsync(
        string userId,
        string action,
        string? ipAddress = null,
        string? userAgent = null,
        string? details = null,
        string status = "SUCCESS",
        string? errorMessage = null,
        string? resourceId = null,
        string? resourceType = null)
    {
        try
        {
            var auditLog = AuditLog.Create(
                userId,
                action,
                ipAddress,
                userAgent,
                details,
                status,
                errorMessage,
                resourceId,
                resourceType
            );

            _dbContext.Set<AuditLog>().Add(auditLog);
            var saved = await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Audit Log guardado: {Action} - Usuario: {UserId} - Status: {Status} (Registros: {Count})",
                action, userId, status, saved);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error guardando audit log: {Action} - {UserId}", action, userId);
        }
    }

    public async Task LogLoginAsync(string userId, string ipAddress, string? userAgent = null)
    {
        await LogActionAsync(userId, "LOGIN", ipAddress, userAgent, status: "SUCCESS");
    }

    public async Task LogFailedLoginAsync(string userId, string ipAddress, string? userAgent = null, string? error = null)
    {
        await LogActionAsync(userId, "FAILED_LOGIN", ipAddress, userAgent, status: "FAILED", errorMessage: error);
    }
}

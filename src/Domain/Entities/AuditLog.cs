namespace SAPFIAI.Domain.Entities;

/// <summary>
/// Entidad para registrar auditoría de acciones y accesos del sistema
/// </summary>
public class AuditLog : BaseEntity
{
    public string UserId { get; private set; }
    public string Action { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTime Timestamp { get; private set; }
    public string? Details { get; private set; }
    public string Status { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? ResourceId { get; private set; }
    public string? ResourceType { get; private set; }

    private AuditLog(string userId, string action, string? ipAddress, string? userAgent, string? details, string status, string? errorMessage, string? resourceId, string? resourceType)
    {
        UserId = userId;
        Action = action;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        Timestamp = DateTime.UtcNow;
        Details = details;
        Status = status;
        ErrorMessage = errorMessage;
        ResourceId = resourceId;
        ResourceType = resourceType;
    }

    // Required by EF Core
    private AuditLog() 
    {
        UserId = string.Empty;
        Action = string.Empty;
        Status = string.Empty;
    }

    public static AuditLog Create(string userId, string action, string? ipAddress, string? userAgent, string? details, string status = "SUCCESS", string? errorMessage = null, string? resourceId = null, string? resourceType = null)
    {
        return new AuditLog(userId, action, ipAddress, userAgent, details, status, errorMessage, resourceId, resourceType);
    }
}

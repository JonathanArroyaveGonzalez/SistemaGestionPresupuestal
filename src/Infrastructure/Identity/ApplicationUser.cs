using Microsoft.AspNetCore.Identity;
using SAPFIAI.Domain.Entities;

namespace SAPFIAI.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public DateTime? LastLoginDate { get; set; }

    public string? LastLoginIp { get; set; }

    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}

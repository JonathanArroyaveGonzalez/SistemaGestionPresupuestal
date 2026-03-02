using SAPFIAI.Domain.Entities;

namespace SAPFIAI.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<AuditLog> AuditLogs { get; }

    DbSet<Permission> Permissions { get; }

    DbSet<RolePermission> RolePermissions { get; }

    DbSet<RefreshToken> RefreshTokens { get; }

    DbSet<IpBlackList> IpBlackLists { get; }

    DbSet<LoginAttempt> LoginAttempts { get; }

    DbSet<T> Set<T>() where T : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

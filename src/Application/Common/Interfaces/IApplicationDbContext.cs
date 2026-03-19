using SAPFIAI.Domain.Entities;

namespace SAPFIAI.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<AuditLog> AuditLogs { get; }

    DbSet<RefreshToken> RefreshTokens { get; }

    DbSet<T> Set<T>() where T : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

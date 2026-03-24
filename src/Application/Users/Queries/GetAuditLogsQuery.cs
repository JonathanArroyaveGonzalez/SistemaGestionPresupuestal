using SAPFIAI.Application.Common.Interfaces;
using SAPFIAI.Application.Common.Models;

namespace SAPFIAI.Application.Users.Queries;

public record GetAuditLogsQuery : IRequest<PagedResult<AuditLogDto>>
{
    public string? UserId { get; init; }

    public string? Action { get; init; }

    public DateTime? FromDate { get; init; }

    public DateTime? ToDate { get; init; }

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}

public class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, PagedResult<AuditLogDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAuditLogsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (!string.IsNullOrEmpty(request.UserId))
            query = query.Where(x => x.UserId == request.UserId);

        if (!string.IsNullOrEmpty(request.Action))
            query = query.Where(x => x.Action == request.Action);

        if (request.FromDate.HasValue)
            query = query.Where(x => x.Timestamp >= request.FromDate);

        if (request.ToDate.HasValue)
            query = query.Where(x => x.Timestamp <= request.ToDate);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.Timestamp)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new AuditLogDto
            {
                Id = x.Id,
                UserId = x.UserId,
                Action = x.Action,
                IpAddress = x.IpAddress,
                UserAgent = x.UserAgent,
                Timestamp = x.Timestamp,
                Details = x.Details,
                Status = x.Status,
                ErrorMessage = x.ErrorMessage,
                ResourceId = x.ResourceId,
                ResourceType = x.ResourceType
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLogDto>
        {
            Items = items,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }
}

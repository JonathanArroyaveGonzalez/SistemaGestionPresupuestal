using SAPFIAI.Application.Common.Models;

namespace SAPFIAI.Application.Security.Queries.GetBlockedIps;

public record GetBlockedIpsQuery : IRequest<IEnumerable<IpBlackListDto>>
{
    public bool ActiveOnly { get; init; } = true;
}

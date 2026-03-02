using SAPFIAI.Application.Common.Interfaces;
using SAPFIAI.Application.Common.Models;

namespace SAPFIAI.Application.Security.Queries.GetBlockedIps;

public class GetBlockedIpsQueryHandler : IRequestHandler<GetBlockedIpsQuery, IEnumerable<IpBlackListDto>>
{
    private readonly IIpBlackListService _ipBlackListService;

    public GetBlockedIpsQueryHandler(IIpBlackListService ipBlackListService)
    {
        _ipBlackListService = ipBlackListService;
    }

    public async Task<IEnumerable<IpBlackListDto>> Handle(GetBlockedIpsQuery request, CancellationToken cancellationToken)
    {
        var ips = await _ipBlackListService.GetBlockedIpsAsync(request.ActiveOnly);
        
        return ips.Select(ip => new IpBlackListDto
        {
            Id = ip.Id,
            IpAddress = ip.IpAddress,
            Reason = ip.Reason,
            IsActive = ip.IsActive,
            ExpiryDate = ip.ExpiryDate,
            CreatedAt = ip.BlockedDate
        });
    }
}

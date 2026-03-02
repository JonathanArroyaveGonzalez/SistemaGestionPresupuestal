using SAPFIAI.Application.Common.Interfaces;

namespace SAPFIAI.Web.Services;

public class HttpContextInfo : IHttpContextInfo
{
    private readonly IHttpContextAccessor _accessor;

    public HttpContextInfo(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public string? IpAddress => _accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent => _accessor.HttpContext?.Request.Headers.UserAgent.ToString();
}

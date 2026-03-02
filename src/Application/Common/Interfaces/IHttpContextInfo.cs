namespace SAPFIAI.Application.Common.Interfaces;

public interface IHttpContextInfo
{
    string? IpAddress { get; }
    string? UserAgent { get; }
}

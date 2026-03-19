namespace SAPFIAI.Application.Common.Models;

public sealed record PermitUserSyncRequest
{
    public required string UserId { get; init; }

    public required string Email { get; init; }

    public string? UserName { get; init; }

    public string? PhoneNumber { get; init; }
}
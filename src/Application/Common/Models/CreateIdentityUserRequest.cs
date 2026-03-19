namespace SAPFIAI.Application.Common.Models;

public sealed record CreateIdentityUserRequest
{
    public required string Email { get; init; }

    public required string Password { get; init; }

    public string? UserName { get; init; }

    public string? PhoneNumber { get; init; }
}
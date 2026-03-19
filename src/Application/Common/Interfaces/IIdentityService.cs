using SAPFIAI.Application.Common.Models;

namespace SAPFIAI.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<string?> GetUserNameAsync(string userId);

    Task<(Result Result, string UserId)> CreateUserAsync(CreateIdentityUserRequest request);

    Task<Result> DeleteUserAsync(string userId);

    Task<(bool Success, string? Token)> GeneratePasswordResetTokenAsync(string email);

    Task<Result> ResetPasswordAsync(string email, string token, string newPassword);

    Task<Result> ChangePasswordAsync(string userId, string currentPassword, string newPassword);

    Task<Result> SetTwoFactorEnabledAsync(string userId, bool enabled);

    Task<bool> CheckPasswordAsync(string userId, string password);
}

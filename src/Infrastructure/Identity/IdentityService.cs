using SAPFIAI.Application.Common.Interfaces;
using SAPFIAI.Application.Common.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace SAPFIAI.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IdentityService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<string?> GetUserNameAsync(string userId)
    {
        var user = await _userManager.Users.FirstAsync(u => u.Id == userId);
        return user.UserName;
    }

    public async Task<(Result Result, string UserId)> CreateUserAsync(CreateIdentityUserRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = string.IsNullOrWhiteSpace(request.UserName) ? request.Email : request.UserName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        return (result.ToApplicationResult(), user.Id);
    }

    public async Task<Result> DeleteUserAsync(string userId)
    {
        var user = _userManager.Users.SingleOrDefault(u => u.Id == userId);
        return user != null ? await DeleteUserAsync(user) : Result.Success();
    }

    public async Task<Result> DeleteUserAsync(ApplicationUser user)
    {
        var result = await _userManager.DeleteAsync(user);
        return result.ToApplicationResult();
    }

    public async Task<(bool Success, string? UserId)> GetUserIdByEmailAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user == null ? (false, null) : (true, user.Id);
    }

    public async Task<(bool Success, string? Token)> GeneratePasswordResetTokenAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return (false, null);

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        return (true, token);
    }

    public async Task<Result> ResetPasswordAsync(string email, string token, string newPassword)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return Result.Failure(new Error("UserNotFound", "Usuario no encontrado"));

        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        return result.ToApplicationResult();
    }

    public async Task<Result> ResetPasswordByUserIdAsync(string userId, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return Result.Failure(new Error("UserNotFound", "Usuario no encontrado"));

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        return result.ToApplicationResult();
    }

    public async Task<Result> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return Result.Failure(new Error("UserNotFound", "Usuario no encontrado"));

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        return result.ToApplicationResult();
    }

    public async Task<Result> SetTwoFactorEnabledAsync(string userId, bool enabled)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return Result.Failure(new Error("UserNotFound", "Usuario no encontrado"));

        var result = await _userManager.SetTwoFactorEnabledAsync(user, enabled);
        return result.ToApplicationResult();
    }

    public async Task<bool> CheckPasswordAsync(string userId, string password)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return false;

        return await _userManager.CheckPasswordAsync(user, password);
    }
}

using SAPFIAI.Application.Common.Interfaces;
using SAPFIAI.Application.Common.Models;
using SAPFIAI.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace SAPFIAI.Infrastructure.Services;

public class AuthenticationOperations : IAuthenticationOperations
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AuthenticationOperations> _logger;

    public AuthenticationOperations(
        UserManager<ApplicationUser> userManager,
        ILogger<AuthenticationOperations> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<(bool IsValid, string? UserId, string? Email)> VerifyCredentialsAsync(string email, string password)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return (false, null, null);

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, password);
            if (!isPasswordValid)
                return (false, null, null);

            return (true, user.Id, user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying credentials for {Email}", email);
            return (false, null, null);
        }
    }

    public async Task<UserDto?> GetUserByIdAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return null;

            return new UserDto
            {
                Id = user.Id,
                Email = user.Email!,
                UserName = user.UserName,
                PhoneNumber = user.PhoneNumber,
                TwoFactorEnabled = user.TwoFactorEnabled,
                LastLoginDate = user.LastLoginDate,
                LastLoginIp = user.LastLoginIp
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by ID {UserId}", userId);
            return null;
        }
    }

    public async Task<UserDto?> GetUserByEmailAsync(string email)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return null;

            return new UserDto
            {
                Id = user.Id,
                Email = user.Email!,
                UserName = user.UserName,
                PhoneNumber = user.PhoneNumber,
                TwoFactorEnabled = user.TwoFactorEnabled,
                LastLoginDate = user.LastLoginDate,
                LastLoginIp = user.LastLoginIp
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by email {Email}", email);
            return null;
        }
    }

    public async Task UpdateLastLoginAsync(string userId, string? ipAddress)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.LastLoginDate = DateTime.UtcNow;
                user.LastLoginIp = ipAddress;
                await _userManager.UpdateAsync(user);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating last login for user {UserId}", userId);
        }
    }

    public async Task<bool> Has2FAEnabledAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user?.TwoFactorEnabled ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking 2FA status for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> EnableTwoFactorAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return false;

            var result = await _userManager.SetTwoFactorEnabledAsync(user, true);
            return result.Succeeded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling 2FA for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> DisableTwoFactorAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return false;

            var result = await _userManager.SetTwoFactorEnabledAsync(user, false);
            return result.Succeeded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling 2FA for user {UserId}", userId);
            return false;
        }
    }
}

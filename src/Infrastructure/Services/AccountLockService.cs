using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using SAPFIAI.Application.Common.Interfaces;
using SAPFIAI.Infrastructure.Identity;

namespace SAPFIAI.Infrastructure.Services;

public class AccountLockService : IAccountLockService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;

    public AccountLockService(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }

    public async Task<bool> LockAccountAsync(string userId, int lockoutMinutes = 15)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return false;

        await _userManager.SetLockoutEndDateAsync(user, DateTime.UtcNow.AddMinutes(lockoutMinutes));
        await _userManager.SetLockoutEnabledAsync(user, true);

        return true;
    }

    public async Task<bool> UnlockAccountAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return false;

        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);

        return true;
    }

    public async Task<bool> IsAccountLockedAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return false;

        return await _userManager.IsLockedOutAsync(user);
    }

    public async Task<int> IncrementFailedAttemptsAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return 0;

        await _userManager.AccessFailedAsync(user);

        var maxAttempts = _configuration.GetValue<int>("Security:AccountLock:MaxFailedAttempts", 5);
        var failedCount = await _userManager.GetAccessFailedCountAsync(user);

        if (failedCount >= maxAttempts)
        {
            var lockoutMinutes = _configuration.GetValue<int>("Security:AccountLock:LockoutMinutes", 15);
            await LockAccountAsync(userId, lockoutMinutes);
        }

        return failedCount;
    }

    public async Task ResetFailedAttemptsAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return;

        await _userManager.ResetAccessFailedCountAsync(user);
    }

    public async Task<(bool isLocked, DateTime? lockoutEnd, int failedAttempts)> GetAccountLockStatusAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return (false, null, 0);

        var isLocked = await _userManager.IsLockedOutAsync(user);
        var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
        var failedCount = await _userManager.GetAccessFailedCountAsync(user);

        return (isLocked, lockoutEnd?.UtcDateTime, failedCount);
    }
}

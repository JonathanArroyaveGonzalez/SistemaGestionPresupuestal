using System.Security.Cryptography;
using System.Text;
using SAPFIAI.Application.Common.Interfaces;
using SAPFIAI.Infrastructure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SAPFIAI.Infrastructure.Services;

public class TwoFactorService : ITwoFactorService
{
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<TwoFactorService> _logger;
    private readonly IMemoryCache _cache;
    private const string CacheKeyPrefix = "TwoFactorCode";

    public TwoFactorService(
        IEmailService emailService,
        IConfiguration configuration,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment environment,
        ILogger<TwoFactorService> logger,
        IMemoryCache cache)
    {
        _emailService = emailService;
        _configuration = configuration;
        _userManager = userManager;
        _environment = environment;
        _logger = logger;
        _cache = cache;
    }

    private string? GetConfigValue(string key) =>
        _configuration[key] ?? Environment.GetEnvironmentVariable(key);

    public async Task<bool> GenerateAndSendTwoFactorCodeAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return false;

            var code = GenerateRandomCode(6);

            var expirationConfig = GetConfigValue("TWO_FACTOR_EXPIRATION_MINUTES");
            var expirationMinutes = int.Parse(expirationConfig ?? "10");

            _cache.Set(GetCacheKey(userId), code, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(expirationMinutes)
            });

            if (_environment.IsDevelopment())
            {
                _logger.LogWarning("2FA CODE (DEV ONLY): {Code} | User: {Email} | Expires: {Minutes}min", code, user.Email, expirationMinutes);
            }

            var emailSent = await _emailService.SendTwoFactorCodeAsync(user.Email!, code, user.UserName ?? user.Email!);

            if (_environment.IsDevelopment() && !emailSent)
            {
                _logger.LogWarning("Email not sent, but dev mode allows continuing with code shown above.");
                return true;
            }

            return emailSent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating 2FA code");
            return false;
        }
    }

    public async Task<bool> ValidateTwoFactorCodeAsync(string userId, string code)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return false;

            if (!_cache.TryGetValue(GetCacheKey(userId), out string? cachedCode) || string.IsNullOrEmpty(cachedCode))
                return false;

            var expectedBytes = Encoding.UTF8.GetBytes(cachedCode);
            var providedBytes = Encoding.UTF8.GetBytes(code.Trim());

            return expectedBytes.Length == providedBytes.Length &&
                   CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating 2FA code");
            return false;
        }
    }

    public Task ClearTwoFactorCodeAsync(string userId)
    {
        try { _cache.Remove(GetCacheKey(userId)); }
        catch (Exception ex) { _logger.LogError(ex, "Error clearing 2FA code"); }
        return Task.CompletedTask;
    }

    public async Task<bool> IsTwoFactorEnabledAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user?.TwoFactorEnabled ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking 2FA status");
            return false;
        }
    }

    private static string GenerateRandomCode(int length)
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[length];
        rng.GetBytes(bytes);
        return string.Concat(bytes.Select(b => (b % 10).ToString()));
    }

    private static string GetCacheKey(string userId) => $"{CacheKeyPrefix}:{userId}";
}

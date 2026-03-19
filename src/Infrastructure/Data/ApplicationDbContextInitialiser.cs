using SAPFIAI.Application.Common.Interfaces;
using SAPFIAI.Application.Common.Models;
using SAPFIAI.Domain.Constants;
using SAPFIAI.Infrastructure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SAPFIAI.Infrastructure.Data;

public static class InitialiserExtensions
{
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();
        await initialiser.InitialiseAsync();
        await initialiser.SeedAsync();
    }
}

public class ApplicationDbContextInitialiser
{
    private readonly ILogger<ApplicationDbContextInitialiser> _logger;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public ApplicationDbContextInitialiser(
        ILogger<ApplicationDbContextInitialiser> logger,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    public async Task InitialiseAsync()
    {
        try
        {
            var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                _logger.LogInformation("Applying {Count} pending migrations...", pendingMigrations.Count());
                await _context.Database.MigrateAsync();
            }
            else
            {
                _logger.LogInformation("Database is up to date. No pending migrations.");
            }
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 2714)
        {
            _logger.LogWarning("Database tables already exist. Skipping migration.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initialising the database.");
            throw;
        }
    }

    public async Task SeedAsync()
    {
        try
        {
            await TrySeedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    public async Task TrySeedAsync()
    {
        var adminEmail = _configuration["BootstrapAdmin:Email"] ?? "administrator@localhost";
        var adminPassword = _configuration["BootstrapAdmin:Password"] ?? "Administrator1!";
        var adminUserName = _configuration["BootstrapAdmin:UserName"] ?? adminEmail;
        var adminPhoneNumber = _configuration["BootstrapAdmin:PhoneNumber"];
        var skipPermitBootstrap = _configuration.GetValue<bool>("SkipPermitBootstrap");

        var administrator = await _userManager.FindByEmailAsync(adminEmail);

        if (administrator == null)
        {
            administrator = new ApplicationUser
            {
                UserName = adminUserName,
                Email = adminEmail,
                PhoneNumber = adminPhoneNumber
            };

            var createResult = await _userManager.CreateAsync(administrator, adminPassword);
            if (!createResult.Succeeded)
            {
                var error = string.Join("; ", createResult.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Could not create bootstrap admin user: {error}");
            }
        }

        if (skipPermitBootstrap)
        {
            _logger.LogWarning("SkipPermitBootstrap is enabled. Permit.io bootstrap was skipped for the initial administrator.");
            return;
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var permitProvisioning = scope.ServiceProvider.GetRequiredService<IPermitProvisioningService>();

            await permitProvisioning.EnsureAuthorizationModelAsync();
            await permitProvisioning.SyncUserAsync(new PermitUserSyncRequest
            {
                UserId = administrator.Id,
                Email = administrator.Email ?? adminEmail,
                UserName = administrator.UserName,
                PhoneNumber = administrator.PhoneNumber
            });
            await permitProvisioning.AssignRoleAsync(administrator.Id, PermitConstants.Roles.Admin);

            _logger.LogInformation("Permit.io bootstrap completed. Admin synced and role assigned.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Permit.io bootstrap failed ({ExceptionType}: {Message}). " +
                "The application will start, but the authorization model may not be fully provisioned. " +
                "Re-run the application to retry the bootstrap.",
                ex.GetType().Name, ex.Message);
        }
    }
}

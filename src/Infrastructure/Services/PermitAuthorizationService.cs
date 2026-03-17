using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PermitSDK;
using SAPFIAI.Application.Common.Interfaces;

namespace SAPFIAI.Infrastructure.Services;

public class PermitAuthorizationService : IPermitAuthorizationService
{
    private readonly Permit _permit;
    private readonly ILogger<PermitAuthorizationService> _logger;

    public Permit Client => _permit;
    public string ProjectId { get; }
    public string EnvironmentId { get; }

    public PermitAuthorizationService(IConfiguration configuration, IHostEnvironment environment, ILogger<PermitAuthorizationService> logger)
    {
        _logger = logger;

        var envSuffix = environment.IsProduction() ? "PRODUCTION" : "DEVELOPMENT";

        var apiKey = configuration[$"PermitIo:EnvironmentKey_{envSuffix}"]
            ?? configuration["PermitIo:EnvironmentKey"]
            ?? throw new InvalidOperationException($"Permit.io key not configured. Set 'PermitIo:EnvironmentKey_{envSuffix}'.");

        var pdpUrl = configuration["PermitIo:PdpUrl"] ?? "https://cloudpdp.api.permit.io";

        ProjectId = configuration["PermitIo:ProjectId"]
            ?? throw new InvalidOperationException("Permit.io ProjectId not configured. Set 'PermitIo:ProjectId'.");

        EnvironmentId = configuration[$"PermitIo:EnvironmentId_{envSuffix}"]
            ?? configuration["PermitIo:EnvironmentId"]
            ?? throw new InvalidOperationException($"Permit.io EnvironmentId not configured. Set 'PermitIo:EnvironmentId_{envSuffix}'.");

        _logger.LogInformation("Permit.io iniciado — entorno: {Env}, project: {Proj}, environment: {EnvId}", envSuffix, ProjectId, EnvironmentId);
        _permit = new Permit(apiKey, pdpUrl);
    }

    public async Task<bool> IsAllowedAsync(string userId, string action, string resource)
    {
        try
        {
            var allowed = await _permit.Check(userId, action, resource);
            _logger.LogDebug("Permit.io check: user={UserId} action={Action} resource={Resource} => {Result}",
                userId, action, resource, allowed);
            return allowed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Permit.io check failed for user={UserId} action={Action} resource={Resource}. Denying by default.",
                userId, action, resource);
            return false;
        }
    }
}

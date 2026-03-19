using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PermitSDK;
using SAPFIAI.Application.Common.Interfaces;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SAPFIAI.Infrastructure.Services;

public class PermitAuthorizationService : IPermitAuthorizationService
{
    private readonly Permit _permit;
    private readonly ILogger<PermitAuthorizationService> _logger;
    private readonly int _checkRetries;
    private readonly int _initialBackoffMs;
    private readonly HttpClient _http;
    private readonly string _apiKey;

    public Permit Client => _permit;
    public string ProjectId { get; }
    public string EnvironmentId { get; }

    public PermitAuthorizationService(IConfiguration configuration, IHostEnvironment environment, ILogger<PermitAuthorizationService> logger)
    {
        _logger = logger;

        var envSuffix = environment.IsProduction() ? "PRODUCTION" : "DEVELOPMENT";

        // PERMITIO__ env vars map to "Permitio:" section (case-preserved by ASP.NET Core)
        var apiKey = GetConfig(configuration, $"Permitio:EnvironmentKey_{envSuffix}")
            ?? GetConfig(configuration, $"PermitIo:EnvironmentKey_{envSuffix}")
            ?? throw new InvalidOperationException($"Permit.io key not configured. Set 'PERMITIO__ENVIRONMENTKEY_{envSuffix}' env var.");

        var pdpUrl = GetConfig(configuration, "Permitio:PdpUrl")
            ?? GetConfig(configuration, "PermitIo:PdpUrl")
            ?? "https://cloudpdp.api.permit.io";

        ProjectId = GetConfig(configuration, "Permitio:ProjectId")
            ?? GetConfig(configuration, "PermitIo:ProjectId")
            ?? throw new InvalidOperationException("Permit.io ProjectId not configured. Set 'PERMITIO__PROJECTID' env var.");

        EnvironmentId = GetConfig(configuration, $"Permitio:EnvironmentId_{envSuffix}")
            ?? GetConfig(configuration, $"PermitIo:EnvironmentId_{envSuffix}")
            ?? throw new InvalidOperationException($"Permit.io EnvironmentId not configured. Set 'PERMITIO__ENVIRONMENTID_{envSuffix}' env var.");

        _checkRetries = Math.Max(0, configuration.GetValue<int?>("Permitio:CheckRetries") ?? 3);
        _initialBackoffMs = Math.Max(100, configuration.GetValue<int?>("Permitio:CheckInitialBackoffMs") ?? 250);

        _apiKey = apiKey;
        _logger.LogInformation("Permit.io iniciado — entorno: {Env}, project: {Proj}, environment: {EnvId}", envSuffix, ProjectId, EnvironmentId);
        _permit = new Permit(apiKey, pdpUrl);

        _http = new HttpClient { BaseAddress = new Uri("https://api.permit.io") };
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <summary>GET /v2/schema/{project}/{env}/{path}</summary>
    public async Task<T?> GetSchemaAsync<T>(string path, CancellationToken ct = default)
    {
        var url = $"/v2/schema/{ProjectId}/{EnvironmentId}/{path}";
        var response = await _http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
    }

    public async Task<bool> IsAllowedAsync(string userId, string action, string resource)
    {
        Exception? lastException = null;

        for (var attempt = 1; attempt <= _checkRetries + 1; attempt++)
        {
            try
            {
                var allowed = await _permit.Check(userId, action, resource);
                _logger.LogDebug("Permit.io check: user={UserId} action={Action} resource={Resource} => {Result}",
                    userId, action, resource, allowed);
                return allowed;
            }
            catch (Exception ex) when (IsTransient(ex) && attempt <= _checkRetries)
            {
                lastException = ex;
                var delayMs = _initialBackoffMs * (1 << (attempt - 1));

                _logger.LogWarning(ex,
                    "Permit.io transient failure (attempt {Attempt}/{MaxAttempts}) for user={UserId} action={Action} resource={Resource}. Retrying in {Delay}ms.",
                    attempt,
                    _checkRetries + 1,
                    userId,
                    action,
                    resource,
                    delayMs);

                await Task.Delay(delayMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Permit.io check failed for user={UserId} action={Action} resource={Resource}. Denying by default.",
                    userId,
                    action,
                    resource);
                return false;
            }
        }

        _logger.LogError(lastException,
            "Permit.io check exhausted retries for user={UserId} action={Action} resource={Resource}. Denying by default.",
            userId,
            action,
            resource);
        return false;
    }

    private static string? GetConfig(IConfiguration cfg, string key) =>
        string.IsNullOrWhiteSpace(cfg[key]) ? null : cfg[key];

    private static bool IsTransient(Exception exception)
    {
        return exception is HttpRequestException
               || exception is SocketException
               || exception.InnerException is SocketException
               || exception.InnerException is HttpRequestException
               || exception.Message.Contains("No such host is known", StringComparison.OrdinalIgnoreCase)
               || exception.Message.Contains("cloudpdp.api.permit.io", StringComparison.OrdinalIgnoreCase);
    }
}

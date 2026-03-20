using Microsoft.Extensions.Configuration;
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
    private readonly HttpClient _http;        // Management API — api.permit.io
    private readonly HttpClient _pdp;         // Cloud PDP — cloudpdp.api.permit.io
    private readonly string _apiKey;

    public Permit Client => _permit;
    public string ProjectId { get; }
    public string EnvironmentId { get; }

    public PermitAuthorizationService(IConfiguration configuration, ILogger<PermitAuthorizationService> logger)
    {
        _logger = logger;

        // Resolve API key by ASPNETCORE_ENVIRONMENT suffix first, then fall back to generic key.
        // ASPNETCORE_ENVIRONMENT is read directly from the environment variable (not IConfiguration)
        // because launchSettings.json injects it before the process starts and it is always reliable.
        var aspnetEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        var envSuffix = aspnetEnv.ToUpperInvariant(); // e.g. "DEVELOPMENT", "PRODUCTION"

        var apiKey = GetConfig(configuration, $"Permitio:EnvironmentKey_{envSuffix}")
            ?? GetConfig(configuration, $"PermitIo:EnvironmentKey_{envSuffix}")
            ?? GetConfig(configuration, "Permitio:EnvironmentKey")
            ?? GetConfig(configuration, "PermitIo:EnvironmentKey")
            ?? throw new InvalidOperationException(
                $"Permit.io key not configured for environment '{aspnetEnv}'. " +
                $"Set 'PERMITIO__ENVIRONMENTKEY_{envSuffix}' env var.");

        var pdpUrl = GetConfig(configuration, "Permitio:PdpUrl")
            ?? GetConfig(configuration, "PermitIo:PdpUrl")
            ?? "https://cloudpdp.api.permit.io";

        ProjectId = GetConfig(configuration, "Permitio:ProjectId")
            ?? GetConfig(configuration, "PermitIo:ProjectId")
            ?? throw new InvalidOperationException("Permit.io ProjectId not configured. Set 'PERMITIO__PROJECTID' env var.");

        // Derive EnvironmentId from ASPNETCORE_ENVIRONMENT if not explicitly set.
        // Development -> "dev", anything else (Production, Staging) -> lowercase of the env name.
        var explicitEnvId = GetConfig(configuration, "Permitio:EnvironmentId")
            ?? GetConfig(configuration, "PermitIo:EnvironmentId");

        EnvironmentId = explicitEnvId ?? aspnetEnv switch
        {
            "Development" => "dev",
            _ => aspnetEnv.ToLowerInvariant()
        };

        _checkRetries = Math.Max(0, configuration.GetValue<int?>("Permitio:CheckRetries") ?? 3);
        _initialBackoffMs = Math.Max(100, configuration.GetValue<int?>("Permitio:CheckInitialBackoffMs") ?? 250);

        _apiKey = apiKey;
        _logger.LogInformation("Permit.io iniciado — project: {Proj}, environment: {EnvId}, aspnet-env: {AspNetEnv}",
            ProjectId, EnvironmentId, aspnetEnv);
        _permit = new Permit(apiKey, pdpUrl);

        _http = new HttpClient { BaseAddress = new Uri("https://api.permit.io") };
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var pdpHandler = new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(1) };
        _pdp = new HttpClient(pdpHandler) { BaseAddress = new Uri(pdpUrl.TrimEnd('/') + "/") };
        _pdp.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        _pdp.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
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
        // Use the REST API directly instead of the SDK PDP client.
        // The SDK Enforcer creates its HttpClient at construction time and can cache
        // a failed DNS resolution, causing persistent "No such host" errors.
        Exception? lastException = null;

        for (var attempt = 1; attempt <= _checkRetries + 1; attempt++)
        {
            try
            {
                var body = new
                {
                    user = new { key = userId },
                    action,
                    resource = new { type = resource, tenant = "default" }
                };

                var response = await _pdp.PostAsJsonAsync("/allowed", body);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Permit.io /allowed returned {Status} for user={UserId} action={Action} resource={Resource}",
                        (int)response.StatusCode, userId, action, resource);
                    return false;
                }

                var result = await response.Content.ReadFromJsonAsync<PermitAllowedResponse>();
                var allowed = result?.Allow ?? false;

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
                    attempt, _checkRetries + 1, userId, action, resource, delayMs);
                await Task.Delay(delayMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Permit.io check failed for user={UserId} action={Action} resource={Resource}. Denying by default.",
                    userId, action, resource);
                return false;
            }
        }

        _logger.LogError(lastException,
            "Permit.io check exhausted retries for user={UserId} action={Action} resource={Resource}. Denying by default.",
            userId, action, resource);
        return false;
    }

    private static string? GetConfig(IConfiguration cfg, string key)
    {
        var value = cfg[key];
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (value.StartsWith("CHANGE-ME", StringComparison.OrdinalIgnoreCase)) return null;
        return value;
    }

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

internal sealed class PermitAllowedResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("allow")]
    public bool Allow { get; set; }
}

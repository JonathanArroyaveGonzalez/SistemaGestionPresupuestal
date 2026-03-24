using System.Text;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Client;
using SAPFIAI.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SAPFIAI.Infrastructure.Services;

public class MqttEmailService : IEmailService, IAsyncDisposable
{
    private readonly BrevoEmailService _brevo;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MqttEmailService> _logger;
    private readonly IMqttClient _client;
    private readonly SemaphoreSlim _connectLock = new(1, 1);

    private const string TopicPrefix = "sapfiai/email/";

    public MqttEmailService(BrevoEmailService brevo, IConfiguration configuration, ILogger<MqttEmailService> logger)
    {
        _brevo = brevo;
        _configuration = configuration;
        _logger = logger;
        _client = new MqttFactory().CreateMqttClient();
    }

    private MqttClientOptions BuildOptions() =>
        new MqttClientOptionsBuilder()
            .WithTcpServer(
                _configuration["CLUSTER_URL"] ?? Environment.GetEnvironmentVariable("CLUSTER_URL")!,
                int.Parse(_configuration["PORT"] ?? Environment.GetEnvironmentVariable("PORT") ?? "8883"))
            .WithCredentials(
                _configuration["USERNAME_HIVE"] ?? Environment.GetEnvironmentVariable("USERNAME_HIVE")!,
                _configuration["PASSWORD_HIVE"] ?? Environment.GetEnvironmentVariable("PASSWORD_HIVE")!)
            .WithTlsOptions(o => o.UseTls())
            .WithClientId($"sapfiai-pub-{Environment.MachineName}")
            .WithCleanSession()
            .Build();

    public Task<bool> SendTwoFactorCodeAsync(string email, string code, string userName) =>
        PublishOrFallback("2fa", new { email, code, userName },
            () => _brevo.SendTwoFactorCodeAsync(email, code, userName));

    public Task<bool> SendLoginConfirmationAsync(string email, string userName, string ipAddress, DateTime loginDate) =>
        PublishOrFallback("login", new { email, userName, ipAddress, loginDate },
            () => _brevo.SendLoginConfirmationAsync(email, userName, ipAddress, loginDate));

    public Task<bool> SendSecurityAlertAsync(string email, string userName, string action, string ipAddress) =>
        PublishOrFallback("security", new { email, userName, action, ipAddress },
            () => _brevo.SendSecurityAlertAsync(email, userName, action, ipAddress));

    public Task<bool> SendRegistrationConfirmationAsync(string email, string userName) =>
        PublishOrFallback("registration", new { email, userName },
            () => _brevo.SendRegistrationConfirmationAsync(email, userName));

    public Task<bool> SendPasswordResetAsync(string email, string userName, string resetToken) =>
        PublishOrFallback("password-reset", new { email, userName, resetToken },
            () => _brevo.SendPasswordResetAsync(email, userName, resetToken));

    private async Task<bool> PublishOrFallback(string topic, object payload, Func<Task<bool>> fallback)
    {
        try
        {
            await EnsureConnectedAsync();

            var message = new MqttApplicationMessageBuilder()
                .WithTopic($"{TopicPrefix}{topic}")
                .WithPayload(JsonSerializer.Serialize(payload))
                // AtMostOnce (QoS 0): fire-and-forget, sin reentregas automáticas.
                // El subscriber usa AtLeastOnce para garantizar procesamiento,
                // pero el publisher no necesita confirmación — el fallback cubre fallos.
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce)
                // Sin RetainFlag: el broker NO almacena el mensaje.
                // Con RetainFlag=true el broker reenviaría el mensaje a cada nuevo
                // subscriber/reconexión, causando correos duplicados.
                .Build();

            await _client.PublishAsync(message);
            _logger.LogInformation("MQTT publicado en {Topic}", topic);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MQTT no disponible para {Topic}, usando fallback directo", topic);
            return await fallback();
        }
    }

    private async Task EnsureConnectedAsync()
    {
        if (_client.IsConnected) return;

        await _connectLock.WaitAsync();
        try
        {
            if (!_client.IsConnected)
                await _client.ConnectAsync(BuildOptions());
        }
        finally
        {
            _connectLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_client.IsConnected)
            await _client.DisconnectAsync();
        _client.Dispose();
        _connectLock.Dispose();
    }
}

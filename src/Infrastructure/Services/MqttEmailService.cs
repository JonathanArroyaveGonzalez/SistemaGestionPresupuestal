using System.Text;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Client;
using SAPFIAI.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SAPFIAI.Infrastructure.Services;

public class MqttEmailService : IEmailService
{
    private readonly BrevoEmailService _brevo;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MqttEmailService> _logger;

    private const string Broker = "broker.hivemq.com";
    private const int Port = 1883;
    private const string TopicPrefix = "sapfiai/email/";

    public MqttEmailService(BrevoEmailService brevo, IConfiguration configuration, ILogger<MqttEmailService> logger)
    {
        _brevo = brevo;
        _configuration = configuration;
        _logger = logger;
    }

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
            var factory = new MqttFactory();
            using var client = factory.CreateMqttClient();

            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(Broker, Port)
                .WithClientId($"sapfiai-{Guid.NewGuid():N}")
                .WithCleanSession()
                .Build();

            await client.ConnectAsync(options);

            var message = new MqttApplicationMessageBuilder()
                .WithTopic($"{TopicPrefix}{topic}")
                .WithPayload(JsonSerializer.Serialize(payload))
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .WithRetainFlag()
                .Build();

            await client.PublishAsync(message);
            await client.DisconnectAsync();

            _logger.LogInformation("MQTT publicado en {Topic}", topic);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MQTT no disponible para {Topic}, usando fallback directo", topic);
            return await fallback();
        }
    }
}

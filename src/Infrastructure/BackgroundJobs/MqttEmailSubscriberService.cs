using System.Text;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SAPFIAI.Infrastructure.BackgroundJobs;

public class MqttEmailSubscriberService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MqttEmailSubscriberService> _logger;
    private IMqttClient? _client;

    private const string Broker = "broker.hivemq.com";
    private const int Port = 1883;
    private const string TopicFilter = "sapfiai/email/#";

    public MqttEmailSubscriberService(IServiceScopeFactory scopeFactory, ILogger<MqttEmailSubscriberService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new MqttFactory();
        _client = factory.CreateMqttClient();

        _client.ApplicationMessageReceivedAsync += OnMessageReceived;

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(Broker, Port)
            .WithClientId($"sapfiai-subscriber-{Guid.NewGuid():N}")
            .WithCleanSession(false)
            .Build();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_client.IsConnected)
                {
                    await _client.ConnectAsync(options, stoppingToken);
                    await _client.SubscribeAsync(TopicFilter, MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce, stoppingToken);
                    _logger.LogInformation("MQTT subscriber conectado a {Broker}", Broker);
                }
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MQTT subscriber desconectado, reintentando en 15s");
                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }
        }
    }

    private async Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs e)
    {
        var topic = e.ApplicationMessage.Topic;
        var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
        var emailType = topic.Split('/').LastOrDefault();

        _logger.LogInformation("MQTT mensaje recibido en {Topic}", topic);

        using var scope = _scopeFactory.CreateScope();
        var brevo = scope.ServiceProvider.GetRequiredService<SAPFIAI.Infrastructure.Services.BrevoEmailService>();

        try
        {
            var doc = JsonDocument.Parse(payload).RootElement;
            var result = emailType switch
            {
                "2fa" => await brevo.SendTwoFactorCodeAsync(
                    doc.GetProperty("email").GetString()!,
                    doc.GetProperty("code").GetString()!,
                    doc.GetProperty("userName").GetString()!),
                "login" => await brevo.SendLoginConfirmationAsync(
                    doc.GetProperty("email").GetString()!,
                    doc.GetProperty("userName").GetString()!,
                    doc.GetProperty("ipAddress").GetString()!,
                    doc.GetProperty("loginDate").GetDateTime()),
                "security" => await brevo.SendSecurityAlertAsync(
                    doc.GetProperty("email").GetString()!,
                    doc.GetProperty("userName").GetString()!,
                    doc.GetProperty("action").GetString()!,
                    doc.GetProperty("ipAddress").GetString()!),
                "registration" => await brevo.SendRegistrationConfirmationAsync(
                    doc.GetProperty("email").GetString()!,
                    doc.GetProperty("userName").GetString()!),
                "password-reset" => await brevo.SendPasswordResetAsync(
                    doc.GetProperty("email").GetString()!,
                    doc.GetProperty("userName").GetString()!,
                    doc.GetProperty("resetToken").GetString()!),
                _ => false
            };

            if (!result)
                _logger.LogError("Fallo al procesar email MQTT tipo {Type}", emailType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando mensaje MQTT tipo {Type}", emailType);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_client?.IsConnected == true)
            await _client.DisconnectAsync();
        await base.StopAsync(cancellationToken);
    }
}

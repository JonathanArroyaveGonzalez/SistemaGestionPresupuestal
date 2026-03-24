using System.Text;
using System.Text.Json;
using SAPFIAI.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SAPFIAI.Infrastructure.Services;

/// <summary>
/// Implementación de servicio de email usando API REST de Brevo
/// </summary>
public class BrevoEmailService : IEmailService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BrevoEmailService> _logger;
    private const string BrevoApiBaseUrl = "https://api.brevo.com/v3";

    public BrevoEmailService(
        HttpClient httpClient, 
        IConfiguration configuration,
        ILogger<BrevoEmailService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene una variable de configuración desde IConfiguration o Environment
    /// </summary>
    private string? GetConfigValue(string key)
    {
        return _configuration[key] ?? Environment.GetEnvironmentVariable(key);
    }

    public async Task<bool> SendTwoFactorCodeAsync(string email, string code, string userName)
    {
        try
        {
            var apiKey = GetConfigValue("API_KEY_BREVO");
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("API_KEY_BREVO no está configurada en las variables de entorno");
                return false;
            }

            var senderEmail = GetConfigValue("BREVO_SENDER_EMAIL") ?? "noreply@sapfiai.com";
            var senderName = GetConfigValue("BREVO_SENDER_NAME") ?? "SAPFIAI";
            
            var htmlContent = GenerateTwoFactorTemplate(code, userName);

            var payload = new
            {
                sender = new { email = senderEmail, name = senderName },
                to = new[] { new { email, name = userName } },
                subject = "Tu código de verificación de dos factores",
                htmlContent
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{BrevoApiBaseUrl}/smtp/email")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };

            request.Headers.Add("api-key", apiKey);

            _logger.LogInformation("Enviando código 2FA a {Email} desde {SenderEmail}", email, senderEmail);

            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Error de Brevo API: {StatusCode} - {Error}", response.StatusCode, errorContent);
                return false;
            }
            
            _logger.LogInformation("Email 2FA enviado exitosamente a {Email}", email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando email 2FA a {Email}", email);
            return false;
        }
    }

    public async Task<bool> SendLoginConfirmationAsync(string email, string userName, string ipAddress, DateTime loginDate)
    {
        try
        {
            var apiKey = GetConfigValue("API_KEY_BREVO");
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("API_KEY_BREVO no está configurada");
                return false;
            }

            var senderEmail = GetConfigValue("BREVO_SENDER_EMAIL") ?? "noreply@sapfiai.com";
            var senderName = GetConfigValue("BREVO_SENDER_NAME") ?? "SAPFIAI";
            var htmlContent = GenerateLoginConfirmationTemplate(userName, ipAddress, loginDate);

            var payload = new
            {
                sender = new { email = senderEmail, name = senderName },
                to = new[] { new { email, name = userName } },
                subject = "Confirmación de inicio de sesión",
                htmlContent
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{BrevoApiBaseUrl}/smtp/email")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };

            request.Headers.Add("api-key", apiKey);

            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Error de Brevo API: {StatusCode} - {Error}", response.StatusCode, errorContent);
            }
            
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando confirmación de login a {Email}", email);
            return false;
        }
    }

    public async Task<bool> SendSecurityAlertAsync(string email, string userName, string action, string ipAddress)
    {
        try
        {
            var apiKey = GetConfigValue("API_KEY_BREVO");
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("API_KEY_BREVO no está configurada");
                return false;
            }

            var senderEmail = GetConfigValue("BREVO_SENDER_EMAIL") ?? "noreply@sapfiai.com";
            var senderName = GetConfigValue("BREVO_SENDER_NAME") ?? "SAPFIAI Seguridad";
            var htmlContent = GenerateSecurityAlertTemplate(userName, action, ipAddress);


            var payload = new
            {
                sender = new { email = senderEmail, name = senderName },
                to = new[] { new { email, name = userName } },
                subject = "⚠️ Alerta de seguridad en tu cuenta",
                htmlContent
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{BrevoApiBaseUrl}/smtp/email")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };

            request.Headers.Add("api-key", apiKey);

            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Error de Brevo API: {StatusCode} - {Error}", response.StatusCode, errorContent);
            }
            
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando alerta de seguridad a {Email}", email);
            return false;
        }
    }

    /// <summary>
    /// Genera template HTML para código de 2FA
    /// </summary>
    private string GenerateTwoFactorTemplate(string code, string userName)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; background-color: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .header {{ text-align: center; color: #333; }}
        .code-box {{ background-color: #f0f0f0; border: 2px solid #007bff; padding: 20px; text-align: center; border-radius: 8px; margin: 20px 0; }}
        .code {{ font-size: 32px; font-weight: bold; color: #007bff; letter-spacing: 5px; }}
        .info {{ color: #666; font-size: 14px; margin: 15px 0; }}
        .warning {{ color: #d32f2f; font-size: 12px; margin-top: 15px; }}
        .footer {{ text-align: center; color: #999; font-size: 12px; margin-top: 20px; border-top: 1px solid #eee; padding-top: 10px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>Código de Verificación de Dos Factores</h2>
        </div>

        <p>Hola {userName},</p>

        <p class='info'>Se ha solicitado un código de verificación para tu cuenta SAPFIAI. Usa el código a continuación para completar tu acceso:</p>

        <div class='code-box'>
            <div class='code'>{code}</div>
        </div>

        <p class='info'>Este código expirará en 10 minutos.</p>

        <p class='warning'>⚠️ Si tú no solicitaste este código, ignora este correo y asegúrate de cambiar tu contraseña de inmediato.</p>

        <div class='footer'>
            <p>Este es un mensaje automático de SAPFIAI. Por favor, no respondas a este correo.</p>
        </div>
    </div>
</body>
</html>";
    }

    /// <summary>
    /// Genera template HTML para confirmación de login
    /// </summary>
    private string GenerateLoginConfirmationTemplate(string userName, string ipAddress, DateTime loginDate)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; background-color: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .header {{ text-align: center; color: #333; }}
        .info-box {{ background-color: #f0f0f0; padding: 15px; border-radius: 8px; margin: 15px 0; }}
        .info-row {{ margin: 10px 0; }}
        .label {{ font-weight: bold; color: #333; }}
        .footer {{ text-align: center; color: #999; font-size: 12px; margin-top: 20px; border-top: 1px solid #eee; padding-top: 10px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>✓ Sesión Iniciada</h2>
        </div>

        <p>Hola {userName},</p>

        <p>Se ha detectado un nuevo inicio de sesión en tu cuenta.</p>

        <div class='info-box'>
            <div class='info-row'>
                <span class='label'>Fecha y Hora:</span> {loginDate:dd/MM/yyyy HH:mm:ss}
            </div>
            <div class='info-row'>
                <span class='label'>Dirección IP:</span> {ipAddress}
            </div>
        </div>

        <p>Si reconoces esta actividad, no necesitas hacer nada. Si crees que alguien más accedió a tu cuenta, cambia tu contraseña inmediatamente.</p>

        <div class='footer'>
            <p>Este es un mensaje automático de SAPFIAI.</p>
        </div>
    </div>
</body>
</html>";
    }

    /// <summary>
    /// Genera template HTML para alerta de seguridad
    /// </summary>
    private string GenerateSecurityAlertTemplate(string userName, string action, string ipAddress)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; background-color: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .header {{ text-align: center; color: #d32f2f; }}
        .alert-box {{ background-color: #ffebee; border-left: 4px solid #d32f2f; padding: 15px; margin: 15px 0; }}
        .info {{ color: #666; font-size: 14px; margin: 15px 0; }}
        .footer {{ text-align: center; color: #999; font-size: 12px; margin-top: 20px; border-top: 1px solid #eee; padding-top: 10px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>⚠️ Alerta de Seguridad</h2>
        </div>

        <p>Hola {userName},</p>

        <div class='alert-box'>
            <p><strong>Se ha detectado una actividad inusual en tu cuenta:</strong></p>
            <p><strong>Acción:</strong> {action}<br/>
            <strong>Dirección IP:</strong> {ipAddress}<br/>
            <strong>Fecha y Hora:</strong> {DateTime.UtcNow:dd/MM/yyyy HH:mm:ss}</p>
        </div>

        <p class='info'>Si no reconoces esta actividad, por favor:</p>
        <ol>
            <li>Cambia tu contraseña inmediatamente</li>
            <li>Revisa la actividad reciente de tu cuenta</li>
            <li>Contacta al soporte si crees que tu cuenta fue comprometida</li>
        </ol>


        <div class='footer'>
            <p>Este es un mensaje automático de SAPFIAI por seguridad.</p>
        </div>
    </div>
</body>
</html>";
    }

    public async Task<bool> SendRegistrationConfirmationAsync(string email, string userName)
    {
        try
        {
            var apiKey = GetConfigValue("API_KEY_BREVO");
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("API_KEY_BREVO no está configurada");
                return false;
            }

            var senderEmail = GetConfigValue("BREVO_SENDER_EMAIL") ?? "noreply@sapfiai.com";
            var senderName = GetConfigValue("BREVO_SENDER_NAME") ?? "SAPFIAI";
            var htmlContent = GenerateRegistrationConfirmationTemplate(userName);

            var payload = new
            {
                sender = new { email = senderEmail, name = senderName },
                to = new[] { new { email, name = userName } },
                subject = "¡Bienvenido a SAPFIAI!",
                htmlContent
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{BrevoApiBaseUrl}/smtp/email")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };

            request.Headers.Add("api-key", apiKey);

            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Error de Brevo API: {StatusCode} - {Error}", response.StatusCode, errorContent);
            }
            
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando confirmación de registro a {Email}", email);
            return false;
        }
    }

    public async Task<bool> SendPasswordResetAsync(string email, string userName, string resetToken)
    {
        try
        {
            var apiKey = GetConfigValue("API_KEY_BREVO");
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("API_KEY_BREVO no está configurada");
                return false;
            }

            var senderEmail = GetConfigValue("BREVO_SENDER_EMAIL") ?? "noreply@sapfiai.com";
            var senderName = GetConfigValue("BREVO_SENDER_NAME") ?? "SAPFIAI";
            var baseUrl = GetConfigValue("APP__BASEURL") ?? GetConfigValue("App:BaseUrl") ?? "https://localhost:5001";
            var resetUrl = $"{baseUrl}/reset-password?userId={Uri.EscapeDataString(resetToken)}&email={Uri.EscapeDataString(email)}";
            var htmlContent = GeneratePasswordResetTemplate(userName, resetUrl);

            var payload = new
            {
                sender = new { email = senderEmail, name = senderName },
                to = new[] { new { email, name = userName } },
                subject = "Restablecer tu contraseña",
                htmlContent
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{BrevoApiBaseUrl}/smtp/email")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };

            request.Headers.Add("api-key", apiKey);

            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Error de Brevo API: {StatusCode} - {Error}", response.StatusCode, errorContent);
            }
            
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando email de reset de contraseña a {Email}", email);
            return false;
        }
    }

    private string GenerateRegistrationConfirmationTemplate(string userName)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; background-color: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .header {{ text-align: center; color: #4caf50; }}
        .content {{ color: #666; font-size: 14px; margin: 15px 0; }}
        .footer {{ text-align: center; color: #999; font-size: 12px; margin-top: 20px; border-top: 1px solid #eee; padding-top: 10px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>🎉 ¡Bienvenido a SAPFIAI!</h2>
        </div>

        <p>Hola {userName},</p>

        <p class='content'>Tu cuenta ha sido creada exitosamente. Ya puedes iniciar sesión y comenzar a usar nuestros servicios.</p>

        <p class='content'>Si tienes alguna pregunta, no dudes en contactarnos.</p>

        <div class='footer'>
            <p>Este es un mensaje automático de SAPFIAI.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GeneratePasswordResetTemplate(string userName, string resetUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; background-color: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .header {{ text-align: center; color: #ff9800; }}
        .content {{ color: #666; font-size: 14px; margin: 15px 0; }}
        .btn {{ display: inline-block; background-color: #007bff; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; margin: 20px 0; }}
        .warning {{ color: #d32f2f; font-size: 12px; margin-top: 15px; }}
        .footer {{ text-align: center; color: #999; font-size: 12px; margin-top: 20px; border-top: 1px solid #eee; padding-top: 10px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>🔐 Restablecer Contraseña</h2>
        </div>

        <p>Hola {userName},</p>

        <p class='content'>Recibimos una solicitud para restablecer la contraseña de tu cuenta.</p>

        <p style='text-align: center;'>
            <a href='{resetUrl}' class='btn'>Restablecer Contraseña</a>
        </p>

        <p class='content'>Este enlace expirará en 24 horas.</p>

        <p class='warning'>⚠️ Si no solicitaste este cambio, ignora este correo. Tu contraseña no será modificada.</p>

        <div class='footer'>
            <p>Este es un mensaje automático de SAPFIAI.</p>
        </div>
    </div>
</body>
</html>";
    }
}

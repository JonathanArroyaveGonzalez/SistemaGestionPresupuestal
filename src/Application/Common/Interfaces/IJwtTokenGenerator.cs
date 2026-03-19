namespace SAPFIAI.Application.Common.Interfaces;

/// <summary>
/// Interfaz para generaci�n de tokens JWT
/// </summary>
public interface IJwtTokenGenerator
{
    /// <summary>
    /// Genera un token JWT para el usuario
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <param name="email">Email del usuario</param>
    /// <param name="requiresTwoFactorVerification">Indica si el token requiere verificaci�n 2FA</param>
    /// <returns>Token JWT como string</returns>
    string GenerateToken(string userId, string email, bool requiresTwoFactorVerification = false);

    /// <summary>
    /// Valida si un token tiene pendiente la verificaci�n 2FA
    /// </summary>
    bool RequiresTwoFactorVerification(string token);

    /// <summary>
    /// Obtiene el UserId de un token
    /// </summary>
    string? GetUserIdFromToken(string token);

    /// <summary>
    /// Genera un token de refresco
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Valida un token JWT y devuelve si es v�lido
    /// </summary>
    bool ValidateToken(string token);

    /// <summary>
    /// Obtiene el email de un token
    /// </summary>
    string? GetEmailFromToken(string token);
}



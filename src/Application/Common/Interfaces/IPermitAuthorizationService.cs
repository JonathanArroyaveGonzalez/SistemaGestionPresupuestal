namespace SAPFIAI.Application.Common.Interfaces;

public interface IPermitAuthorizationService
{
    /// <summary>
    /// Verifica si un usuario tiene permiso para ejecutar una acción sobre un recurso.
    /// </summary>
    /// <param name="userId">ID del usuario (subject en Permit.io)</param>
    /// <param name="action">Acción a verificar (ej: "read", "create", "delete")</param>
    /// <param name="resource">Tipo de recurso (ej: "users", "audit-logs")</param>
    Task<bool> IsAllowedAsync(string userId, string action, string resource);
}

namespace SAPFIAI.Application.Common.Interfaces;

public interface IPermitSetupService
{
    /// <summary>
    /// Configura recursos, roles y pol\u00edticas en Permit.io si no existen.
    /// Idempotente: puede llamarse en cada arranque sin duplicar datos.
    /// </summary>
    Task SetupAsync();
}

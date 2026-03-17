using Microsoft.Extensions.Logging;
using PermitSDK.OpenAPI.Models;
using SAPFIAI.Application.Common.Interfaces;
using DomainRoles = SAPFIAI.Domain.Constants.Roles;

namespace SAPFIAI.Infrastructure.Services;

/// <summary>
/// Configura el modelo de permisos en Permit.io al arrancar la aplicación.
/// Idempotente: si los recursos/roles/políticas ya existen, los omite sin error.
/// Elimina la necesidad de configurar nada manualmente en el portal de Permit.io.
/// </summary>
public class PermitSetupService : IPermitSetupService
{
    private readonly PermitAuthorizationService _permit;
    private readonly ILogger<PermitSetupService> _logger;

    private static readonly Dictionary<string, string[]> Resources = new()
    {
        ["authentication"] = ["read", "create", "update", "delete"]
    };

    private static readonly Dictionary<string, string[]> RolePolicies = new()
    {
        [DomainRoles.Administrator] = ["authentication:read", "authentication:create", "authentication:update", "authentication:delete"],
        [DomainRoles.Manager]       = ["authentication:read", "authentication:create"],
        [DomainRoles.User]          = ["authentication:read", "authentication:create"]
    };

    public PermitSetupService(PermitAuthorizationService permit, ILogger<PermitSetupService> logger)
    {
        _permit = permit;
        _logger = logger;
    }

    public async Task SetupAsync()
    {
        _logger.LogInformation("Iniciando configuración de Permit.io...");

        var delays = new[] { 2, 5, 10 };
        foreach (var delay in delays)
        {
            try
            {
                await EnsureResourcesAsync();
                await EnsureRolesAsync();
                await EnsureRolePoliciesAsync();
                _logger.LogInformation("Configuración de Permit.io completada.");
                return;
            }
            catch (Exception ex) when (ex is System.Net.Http.HttpRequestException || ex is System.Net.Sockets.SocketException)
            {
                _logger.LogWarning("Permit.io no disponible, reintentando en {Delay}s...", delay);
                await Task.Delay(TimeSpan.FromSeconds(delay));
            }
        }

        _logger.LogWarning("Permit.io setup omitido tras 3 intentos. Los permisos se verificarán en runtime.");
    }

    private async Task EnsureResourcesAsync()
    {
        foreach (var (resourceKey, actions) in Resources)
        {
            bool exists;
            try
            {
                await _permit.Client.Api.GetResource(resourceKey);
                exists = true;
            }
            catch (Exception ex) when (ex is not System.Net.Http.HttpRequestException && ex is not System.Net.Sockets.SocketException)
            {
                exists = false;
            }

            if (exists)
            {
                _logger.LogDebug("Recurso '{Resource}' ya existe en Permit.io.", resourceKey);
                continue;
            }

            var actionDict = actions.ToDictionary(
                a => a,
                a => new ActionBlockEditable { Name = a }
            );

            await _permit.Client.Api.CreateResource(new ResourceCreate
            {
                Key = resourceKey,
                Name = resourceKey,
                Actions = actionDict
            });

            _logger.LogInformation("Recurso '{Resource}' creado con acciones: {Actions}.",
                resourceKey, string.Join(", ", actions));
        }
    }

    private async Task EnsureRolesAsync()
    {
        foreach (var role in RolePolicies.Keys)
        {
            bool exists;
            try
            {
                await _permit.Client.Api.GetRole(role);
                exists = true;
            }
            catch (Exception ex) when (ex is not System.Net.Http.HttpRequestException && ex is not System.Net.Sockets.SocketException)
            {
                exists = false;
            }

            if (exists)
            {
                _logger.LogDebug("Rol '{Role}' ya existe en Permit.io.", role);
                continue;
            }

            await _permit.Client.Api.CreateRole(new RoleCreate { Key = role, Name = role });
            _logger.LogInformation("Rol '{Role}' creado en Permit.io.", role);
        }
    }

    private async Task EnsureRolePoliciesAsync()
    {
        foreach (var (role, permissions) in RolePolicies)
        {
            try
            {
                await _permit.Client.Api.UpdateRole(role, new RoleUpdate
                {
                    Permissions = permissions.ToList()
                });
                _logger.LogInformation("Permisos asignados al rol '{Role}': {Permissions}.",
                    role, string.Join(", ", permissions));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudieron asignar permisos al rol '{Role}'.", role);
            }
        }
    }
}

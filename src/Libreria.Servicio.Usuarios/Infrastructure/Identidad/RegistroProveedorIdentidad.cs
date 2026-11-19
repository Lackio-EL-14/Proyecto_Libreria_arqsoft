using Libreria.Servicio.Usuarios.Application.Ports.Secondary;

namespace Libreria.Servicio.Usuarios.Infrastructure.Identidad;

/// <summary>
/// Registra el adaptador de <see cref="IProveedorIdentidad"/> según <c>ProveedorIdentidad:Modo</c>:
/// <c>Keycloak</c> (por defecto) o <c>EnMemoria</c> (desarrollo sin Keycloak).
/// </summary>
public static class RegistroProveedorIdentidad
{
    public const string ClaveModo = "ProveedorIdentidad:Modo";

    public static IServiceCollection AddProveedorIdentidad(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment entorno)
    {
        var modo = configuration[ClaveModo] ?? "Keycloak";

        if (string.Equals(modo, "EnMemoria", StringComparison.OrdinalIgnoreCase))
        {
            if (!entorno.IsDevelopment())
            {
                throw new InvalidOperationException(
                    "ProveedorIdentidad:Modo = EnMemoria solo se permite en Development.");
            }

            services.AddSingleton<IProveedorIdentidad, ProveedorIdentidadEnMemoria>();
            return services;
        }

        if (!string.Equals(modo, "Keycloak", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"{ClaveModo} debe ser Keycloak o EnMemoria.");
        }

        services.Configure<KeycloakOpciones>(configuration.GetSection(KeycloakOpciones.Seccion));
        services.AddSingleton<CacheTokenKeycloak>();
        services.AddHttpClient<IProveedorIdentidad, KeycloakProveedorIdentidad>(cliente =>
            cliente.Timeout = TimeSpan.FromSeconds(15));

        return services;
    }
}

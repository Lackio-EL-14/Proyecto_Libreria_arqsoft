using System.Net.Http.Headers;
using Libreria.Client.Adapters.Clientes;
using Libreria.Client.Adapters.Productos;
using Libreria.Client.Adapters.Proveedores;
using Libreria.Client.Adapters.Usuarios;
using Libreria.Client.Clients.Common;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Libreria.Client.Adapters.DependencyInjection;

/// <summary>
/// Registro del patrón Adapter (US-140). Un solo <c>HttpClient</c> con nombre «ApiGateway»
/// (<see cref="IHttpClientFactory"/>) apunta a la única URL del Front, <c>Servicios:Gateway:BaseUrl</c>
/// (sección 6.1); el gateway enruta cada <c>api/&lt;recurso&gt;</c> a su microservicio.
/// Cada adaptador se registra detrás de su interfaz destino: las páginas dependen solo de ella.
/// </summary>
public static class ApiGateway
{
    public const string NombreCliente = "ApiGateway";
    public const string BaseUrlClave = "Servicios:Gateway:BaseUrl";
    public const string TimeoutClave = "Servicios:TimeoutSeconds";
    private const int TimeoutPorDefectoSegundos = 30;

    /// <summary>Registra el handler que agrega el token de Keycloak (Bearer) a cada llamada (US-125).</summary>
    public static IServiceCollection AddContextoUsuarioServicios(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.TryAddSingleton(TimeProvider.System);
        services.AddTransient<BearerTokenDelegatingHandler>();
        return services;
    }

    /// <summary>El <c>HttpClient</c> «ApiGateway» que usan los cuatro adaptadores.</summary>
    public static IServiceCollection AddApiGateway(this IServiceCollection services, IConfiguration configuration)
    {
        var baseAddress = LeerUrlDelGateway(configuration);
        var timeout = TimeSpan.FromSeconds(configuration.GetValue(TimeoutClave, TimeoutPorDefectoSegundos));

        services
            .AddHttpClient(NombreCliente, client =>
            {
                client.BaseAddress = baseAddress;
                client.Timeout = timeout;
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .AddHttpMessageHandler<BearerTokenDelegatingHandler>();

        return services;
    }

    // Un adaptador por API consumida desde el Front.
    public static IServiceCollection AddModuloClientes(this IServiceCollection services) =>
        services.AddScoped<IClientesApi, ClientesApiAdapter>();

    public static IServiceCollection AddModuloProveedores(this IServiceCollection services) =>
        services.AddScoped<IProveedoresApi, ProveedoresApiAdapter>();

    public static IServiceCollection AddModuloProductos(this IServiceCollection services) =>
        services.AddScoped<IProductosApi, ProductosApiAdapter>();

    public static IServiceCollection AddModuloUsuarios(this IServiceCollection services) =>
        services.AddScoped<IUsuariosApi, UsuariosApiAdapter>();

    private static Uri LeerUrlDelGateway(IConfiguration configuration)
    {
        var baseUrl = configuration[BaseUrlClave];
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseAddress))
        {
            throw new InvalidOperationException($"Falta configurar {BaseUrlClave} con una URL absoluta.");
        }

        // Con "/" final, las rutas relativas ("api/...") se suman a la base sin perder segmentos.
        return new Uri(baseAddress.AbsoluteUri.TrimEnd('/') + "/");
    }
}

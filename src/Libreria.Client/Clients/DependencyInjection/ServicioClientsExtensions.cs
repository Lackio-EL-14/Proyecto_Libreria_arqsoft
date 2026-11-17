using System.Net.Http.Headers;
using Libreria.Client.Clients.Clientes;
using Libreria.Client.Clients.Common;
using Libreria.Client.Clients.Productos;
using Libreria.Client.Clients.Proveedores;
using Libreria.Client.Clients.Usuarios;

namespace Libreria.Client.Clients.DependencyInjection;

/// <summary>
/// Un método de extensión por módulo (sección 7 del planning). Todos los clientes HTTP
/// apuntan a la única URL del Front: <c>Servicios:Gateway:BaseUrl</c> (sección 6.1);
/// el gateway enruta cada <c>api/&lt;recurso&gt;</c> a su microservicio.
/// </summary>
public static class ServicioClientsExtensions
{
    public const string GatewayBaseUrlClave = "Servicios:Gateway:BaseUrl";
    public const string TimeoutClave = "Servicios:TimeoutSeconds";
    private const int TimeoutPorDefectoSegundos = 30;

    public static IServiceCollection AddContextoUsuarioServicios(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddTransient<UsuarioActualDelegatingHandler>();
        return services;
    }

    // Dueño: Edson (Clientes).
    public static IServiceCollection AddModuloClientes(this IServiceCollection services, IConfiguration configuration) =>
        services.AddClienteDelGateway<IServicioClientesClient, ServicioClientesClient>(configuration);

    // Microservicio relacional de Proveedores.
    public static IServiceCollection AddModuloProveedores(this IServiceCollection services, IConfiguration configuration) =>
        services.AddClienteDelGateway<IServicioProveedoresClient, ServicioProveedoresClient>(configuration);

    // Dueño: Josue (Productos, categorías, marcas e histórico).
    public static IServiceCollection AddModuloProductos(this IServiceCollection services, IConfiguration configuration) =>
        services.AddClienteDelGateway<IServicioProductosClient, ServicioProductosClient>(configuration);

    // Dueño: Grupo 2 (Usuarios).
    public static IServiceCollection AddModuloUsuarios(this IServiceCollection services, IConfiguration configuration) =>
        services.AddClienteDelGateway<IServicioUsuariosClient, ServicioUsuariosClient>(configuration);

    private static IServiceCollection AddClienteDelGateway<TClient, TImplementation>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TClient : class
        where TImplementation : class, TClient
    {
        var baseAddress = LeerUrlDelGateway(configuration);
        var timeout = TimeSpan.FromSeconds(configuration.GetValue(TimeoutClave, TimeoutPorDefectoSegundos));

        services
            .AddHttpClient<TClient, TImplementation>(client =>
            {
                client.BaseAddress = baseAddress;
                client.Timeout = timeout;
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .AddHttpMessageHandler<UsuarioActualDelegatingHandler>();

        return services;
    }

    private static Uri LeerUrlDelGateway(IConfiguration configuration)
    {
        var baseUrl = configuration[GatewayBaseUrlClave];
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseAddress))
        {
            throw new InvalidOperationException($"Falta configurar {GatewayBaseUrlClave} con una URL absoluta.");
        }

        // Con "/" final, las rutas relativas ("api/...") se suman a la base sin perder segmentos.
        return new Uri(baseAddress.AbsoluteUri.TrimEnd('/') + "/");
    }
}

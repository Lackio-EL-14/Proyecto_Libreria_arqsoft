using System.Net.Http.Headers;
using Libreria.Client.Clients.Common;
using Libreria.Client.Clients.Reportes;
using Libreria.Client.Clients.Usuarios;
using Libreria.Client.Clients.Ventas;

namespace Libreria.Client.Clients.DependencyInjection;

public static class ServicioClientsExtensions
{
    private const string ServiciosSection = "Servicios";

    public static IServiceCollection AddContextoUsuarioServicios(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.Configure<UsuarioDesarrolloOptions>(configuration.GetSection(UsuarioDesarrolloOptions.SectionName));
        services.AddTransient<UsuarioActualDelegatingHandler>();
        return services;
    }

    public static IServiceCollection AddServicioVentas(this IServiceCollection services, IConfiguration configuration)
    {
        AddServicioClient<IServicioVentasClient, ServicioVentasClient>(services, configuration, "Ventas");
        return services;
    }

    public static IServiceCollection AddServicioUsuarios(this IServiceCollection services, IConfiguration configuration)
    {
        AddServicioClient<IServicioUsuariosClient, ServicioUsuariosClient>(services, configuration, "Usuarios");
        return services;
    }

    public static IServiceCollection AddServicioReportes(this IServiceCollection services, IConfiguration configuration)
    {
        AddServicioClient<IServicioReportesClient, ServicioReportesClient>(services, configuration, "Reportes");
        return services;
    }

    private static void AddServicioClient<TClient, TImplementation>(
        IServiceCollection services,
        IConfiguration configuration,
        string serviceName)
        where TClient : class
        where TImplementation : class, TClient
    {
        var baseUrl = configuration[$"{ServiciosSection}:{serviceName}:BaseUrl"];
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseAddress))
        {
            throw new InvalidOperationException($"Falta configurar Servicios:{serviceName}:BaseUrl con una URL absoluta.");
        }

        var timeoutSeconds = configuration.GetValue($"{ServiciosSection}:TimeoutSeconds", 10);

        services
            .AddHttpClient<TClient, TImplementation>(client =>
            {
                client.BaseAddress = baseAddress;
                client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .AddHttpMessageHandler<UsuarioActualDelegatingHandler>();
    }
}

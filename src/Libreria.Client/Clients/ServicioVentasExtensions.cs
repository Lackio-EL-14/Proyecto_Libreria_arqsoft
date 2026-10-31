namespace Libreria.Client.Clients;

public static class ServicioVentasExtensions
{
    // Registra el cliente HTTP del Servicio de Ventas. La URL se lee de
    // Servicios:Ventas:BaseUrl (appsettings) y no del código.
    public static IServiceCollection AddServicioVentas(
        this IServiceCollection services, IConfiguration configuration)
    {
        var baseUrl = configuration["Servicios:Ventas:BaseUrl"] ?? "http://localhost:5101";

        services.AddHttpContextAccessor();
        services.AddTransient<UsuarioActualHandler>();

        services
            .AddHttpClient<IVentasApiClient, VentasApiClient>(cliente =>
            {
                cliente.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
                cliente.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<UsuarioActualHandler>();

        return services;
    }
}

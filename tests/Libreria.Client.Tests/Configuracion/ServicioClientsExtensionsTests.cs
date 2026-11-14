using Libreria.Client.Clients.Clientes;
using Libreria.Client.Clients.DependencyInjection;
using Libreria.Client.Clients.Productos;
using Libreria.Client.Clients.Usuarios;
using Libreria.Client.Clients.Ventas;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Libreria.Client.Tests.Configuracion;

/// <summary>US-113: el Front tiene una sola URL (Servicios:Gateway:BaseUrl) y los 4 clientes la usan.</summary>
public sealed class ServicioClientsExtensionsTests
{
    [Fact]
    public void Los_cuatro_clientes_apuntan_al_gateway()
    {
        using var proveedor = CrearProveedor("http://localhost:5080");

        Assert.All(
            new object[]
            {
                proveedor.GetRequiredService<IServicioClientesClient>(),
                proveedor.GetRequiredService<IServicioVentasClient>(),
                proveedor.GetRequiredService<IServicioProductosClient>(),
                proveedor.GetRequiredService<IServicioUsuariosClient>()
            },
            Assert.NotNull);

        var fabrica = proveedor.GetRequiredService<IHttpClientFactory>();
        foreach (var nombre in new[] { nameof(IServicioClientesClient), nameof(IServicioVentasClient), nameof(IServicioProductosClient), nameof(IServicioUsuariosClient) })
        {
            Assert.Equal(new Uri("http://localhost:5080/"), fabrica.CreateClient(nombre).BaseAddress);
        }
    }

    [Fact]
    public void Sin_la_URL_del_gateway_el_arranque_falla_con_un_mensaje_claro()
    {
        var error = Assert.Throws<InvalidOperationException>(() => CrearProveedor(null));

        Assert.Contains(ServicioClientsExtensions.GatewayBaseUrlClave, error.Message);
    }

    private static ServiceProvider CrearProveedor(string? urlGateway)
    {
        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ServicioClientsExtensions.GatewayBaseUrlClave] = urlGateway
            })
            .Build();

        return new ServiceCollection()
            .AddContextoUsuarioServicios()
            .AddModuloClientes(configuracion)
            .AddModuloVentas(configuracion)
            .AddModuloProductos(configuracion)
            .AddModuloUsuarios(configuracion)
            .BuildServiceProvider();
    }
}

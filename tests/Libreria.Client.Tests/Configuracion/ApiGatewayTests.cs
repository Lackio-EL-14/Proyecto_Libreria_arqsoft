using Libreria.Client.Adapters.Clientes;
using Libreria.Client.Adapters.DependencyInjection;
using Libreria.Client.Adapters.Productos;
using Libreria.Client.Adapters.Proveedores;
using Libreria.Client.Adapters.Usuarios;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Libreria.Client.Tests.Configuracion;

/// <summary>
/// US-113 y US-140: el Front tiene una sola URL (Servicios:Gateway:BaseUrl), un HttpClient
/// «ApiGateway» y un adaptador por API detrás de su interfaz destino.
/// </summary>
public sealed class ApiGatewayTests
{
    [Fact]
    public void El_cliente_ApiGateway_apunta_a_la_URL_del_gateway()
    {
        using var proveedor = CrearProveedor("http://localhost:5080");

        var fabrica = proveedor.GetRequiredService<IHttpClientFactory>();

        Assert.Equal(new Uri("http://localhost:5080/"), fabrica.CreateClient(ApiGateway.NombreCliente).BaseAddress);
    }

    [Fact]
    public void Cada_interfaz_destino_se_resuelve_con_su_adaptador()
    {
        using var proveedor = CrearProveedor("http://localhost:5080");
        using var alcance = proveedor.CreateScope();
        var servicios = alcance.ServiceProvider;

        Assert.IsType<ClientesApiAdapter>(servicios.GetRequiredService<IClientesApi>());
        Assert.IsType<ProveedoresApiAdapter>(servicios.GetRequiredService<IProveedoresApi>());
        Assert.IsType<ProductosApiAdapter>(servicios.GetRequiredService<IProductosApi>());
        Assert.IsType<UsuariosApiAdapter>(servicios.GetRequiredService<IUsuariosApi>());
    }

    [Fact]
    public void Sin_la_URL_del_gateway_el_arranque_falla_con_un_mensaje_claro()
    {
        var error = Assert.Throws<InvalidOperationException>(() => CrearProveedor(null));

        Assert.Contains(ApiGateway.BaseUrlClave, error.Message);
    }

    private static ServiceProvider CrearProveedor(string? urlGateway)
    {
        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ApiGateway.BaseUrlClave] = urlGateway
            })
            .Build();

        return new ServiceCollection()
            .AddContextoUsuarioServicios()
            .AddApiGateway(configuracion)
            .AddModuloClientes()
            .AddModuloProveedores()
            .AddModuloProductos()
            .AddModuloUsuarios()
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }
}

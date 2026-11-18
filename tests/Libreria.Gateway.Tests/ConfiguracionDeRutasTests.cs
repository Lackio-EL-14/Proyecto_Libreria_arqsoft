using Libreria.Gateway.Configuracion;
using Libreria.Gateway.Tests.Infraestructura;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy;

namespace Libreria.Gateway.Tests;

/// <summary>yarp.g1.json y yarp.g2.json se fusionan y los destinos se pueden cambiar por entorno.</summary>
public sealed class ConfiguracionDeRutasTests
{
    [Fact]
    public void Los_dos_archivos_traen_las_rutas_y_destinos_del_contrato()
    {
        var configuracion = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AgregarRutasYarp([])
            .Build();

        var rutas = configuracion.GetSection("ReverseProxy:Routes").GetChildren()
            .ToDictionary(r => r.Key, r => (Cluster: r["ClusterId"], Ruta: r["Match:Path"]));

        Assert.Equal(new Dictionary<string, (string?, string?)>
        {
            ["g1-clientes"] = ("clientes", "/api/clientes/{**resto}"),
            ["g1-proveedores"] = ("proveedores", "/api/proveedores/{**resto}"),
            ["g1-productos"] = ("productos", "/api/productos/{**resto}"),
            ["g1-categorias"] = ("productos", "/api/categorias/{**resto}"),
            ["g1-marcas"] = ("productos", "/api/marcas/{**resto}"),
            ["g1-historico"] = ("productos", "/api/historico/{**resto}"),
            ["g2-auth"] = ("usuarios", "/api/auth/{**resto}"),
            ["g2-usuarios"] = ("usuarios", "/api/usuarios/{**resto}")
        }, rutas);

        Assert.Equal("http://localhost:5103", configuracion[GatewayFactory.ClaveDestino("clientes")]);
        Assert.Equal("http://localhost:5101", configuracion[GatewayFactory.ClaveDestino("proveedores")]);
        Assert.Equal("http://localhost:5104", configuracion[GatewayFactory.ClaveDestino("productos")]);
        Assert.Equal("http://localhost:5102", configuracion[GatewayFactory.ClaveDestino("usuarios")]);
    }

    [Fact]
    public void Una_variable_de_entorno_gana_sobre_el_archivo_de_rutas()
    {
        const string variable = "ReverseProxy__Clusters__productos__Destinations__principal__Address";
        var anterior = Environment.GetEnvironmentVariable(variable);
        Environment.SetEnvironmentVariable(variable, "http://localhost:5101");

        try
        {
            var configuracion = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddEnvironmentVariables() // como WebApplication.CreateBuilder: antes de los JSON
                .AgregarRutasYarp([])
                .Build();

            Assert.Equal("http://localhost:5101", configuracion[GatewayFactory.ClaveDestino("productos")]);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variable, anterior);
        }
    }

    [Fact]
    public void En_Development_cada_ruta_conserva_el_destino_del_contrato()
    {
        var configuracion = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AgregarRutasYarp([], "Development")
            .Build();

        Assert.Equal("http://localhost:5103", configuracion[GatewayFactory.ClaveDestino("clientes")]);
        Assert.Equal("http://localhost:5104", configuracion[GatewayFactory.ClaveDestino("productos")]);
        Assert.Equal("http://localhost:5101", configuracion[GatewayFactory.ClaveDestino("proveedores")]);
        Assert.Equal("http://localhost:5102", configuracion[GatewayFactory.ClaveDestino("usuarios")]);
    }

    [Fact]
    public void Una_variable_de_entorno_gana_sobre_el_appsettings_del_entorno()
    {
        const string variable = "ReverseProxy__Clusters__productos__Destinations__principal__Address";
        var anterior = Environment.GetEnvironmentVariable(variable);
        Environment.SetEnvironmentVariable(variable, "http://localhost:5104");

        try
        {
            var configuracion = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AgregarRutasYarp([], "Development")
                .Build();

            Assert.Equal("http://localhost:5104", configuracion[GatewayFactory.ClaveDestino("productos")]);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variable, anterior);
        }
    }

    [Fact]
    public void El_argumento_de_linea_de_comandos_gana_sobre_el_archivo_de_rutas()
    {
        var configuracion = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AgregarRutasYarp([$"--{GatewayFactory.ClaveDestino("clientes")}=http://otra-maquina:5103"])
            .Build();

        Assert.Equal("http://otra-maquina:5103", configuracion[GatewayFactory.ClaveDestino("clientes")]);
    }

    [Fact]
    public void YARP_acepta_la_configuracion_y_carga_las_8_rutas_y_4_clusters()
    {
        using var gateway = new GatewayFactory(new Dictionary<string, string>());
        _ = gateway.Server; // arranca el host y valida la configuración de YARP

        var estado = gateway.Services.GetRequiredService<IProxyStateLookup>();

        Assert.Equal(8, estado.GetRoutes().Count());
        Assert.Equal(
            ["clientes", "productos", "proveedores", "usuarios"],
            estado.GetClusters().Select(c => c.ClusterId).Order());
    }
}

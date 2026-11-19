using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Hosting;
using Libreria.Gateway.Tests.Seguridad;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;

namespace Libreria.Gateway.Tests.Infraestructura;

/// <summary>
/// Gateway real (Program.cs, yarp.g1.json y yarp.g2.json) con los destinos de cada cluster
/// reemplazados por los indicados, igual que se haría con variables de entorno.
/// </summary>
public sealed class GatewayFactory : WebApplicationFactory<Program>
{
    private readonly IReadOnlyDictionary<string, string> _destinos;

    public GatewayFactory(IReadOnlyDictionary<string, string> destinos)
    {
        _destinos = destinos;
    }

    public static string ClaveDestino(string cluster) =>
        $"ReverseProxy:Clusters:{cluster}:Destinations:principal:Address";

    /// <summary>Una dirección en la que no escucha nadie: simula un microservicio caído.</summary>
    public static string DireccionSinServicio()
    {
        using var escucha = new TcpListener(IPAddress.Loopback, 0);
        escucha.Start();
        var puerto = ((IPEndPoint)escucha.LocalEndpoint).Port;
        escucha.Stop();
        return $"http://127.0.0.1:{puerto}";
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(configuracion =>
            configuracion.AddInMemoryCollection(
                _destinos.Select(d => new KeyValuePair<string, string?>(ClaveDestino(d.Key), d.Value))));
        builder.ConfigureTestServices(servicios => servicios.UsarJwtDePrueba());
    }

    /// <summary>Los clientes salen con un token de Administrador; las pruebas de seguridad lo cambian o lo quitan.</summary>
    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        client.DefaultRequestHeaders.Authorization = TokensDePrueba.Bearer(TokensDePrueba.Admin());
    }
}

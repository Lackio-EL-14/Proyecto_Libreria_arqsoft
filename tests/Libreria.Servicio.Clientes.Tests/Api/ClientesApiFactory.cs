using System.Data.Common;
using System.Net;
using System.Net.Sockets;
using Libreria.Servicio.Clientes.Application.Factories;
using Libreria.Servicio.Clientes.Application.Ports.Secondary;
using Libreria.Servicio.Clientes.Tests.Fakes;
using Libreria.Servicio.Clientes.Tests.Integracion;
using Libreria.Servicio.Clientes.Tests.Seguridad;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using Libreria.Servicio.Clientes.Infrastructure.Connection;

namespace Libreria.Servicio.Clientes.Tests.Api;

/// <summary>
/// MS Clientes real (Program.cs, controlador, servicio, validador y traducción de Result)
/// con la BD reemplazada por <see cref="ClienteRepositoryFalso"/>, o por un MySQL que no responde.
/// </summary>
public sealed class ClientesApiFactory : WebApplicationFactory<Program>
{
    private readonly bool _baseDeDatosCaida;

    private ClientesApiFactory(bool baseDeDatosCaida)
    {
        _baseDeDatosCaida = baseDeDatosCaida;
    }

    public ClienteRepositoryFalso Repositorio { get; } = new();

    public static ClientesApiFactory ConRepositorioEnMemoria() => new(baseDeDatosCaida: false);

    public static ClientesApiFactory ConBaseDeDatosCaida() => new(baseDeDatosCaida: true);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // Program.cs exige la cadena al arrancar. El Singleton solo acepta un valor por proceso,
        // así que se usa la misma que las pruebas de MySQL cuando está definida.
        builder.UseSetting(
            "ConnectionStrings:ClientesDb",
            Environment.GetEnvironmentVariable(MySqlFactAttribute.Variable)
                ?? "Server=127.0.0.1;Database=libreria_clientes_pruebas");

        builder.ConfigureTestServices(servicios =>
        {
            servicios.UsarJwtDePrueba();

            if (_baseDeDatosCaida)
            {
                servicios.AddScoped<IDbConnectionFactory, ConexionSinServidor>();
            }
            else
            {
                servicios.AddScoped<ClienteRepositoryFactory>(_ => new ClienteRepositoryFactoryFalsa(Repositorio));
            }
        });
    }

    /// <summary>Los clientes salen con un token de Administrador; las pruebas de seguridad lo cambian o lo quitan.</summary>
    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        client.DefaultRequestHeaders.Authorization = TokensDePrueba.Bearer(TokensDePrueba.Admin());
    }

    /// <summary>Abre MySQL en un puerto donde no escucha nadie: falla igual que con el contenedor apagado.</summary>
    private sealed class ConexionSinServidor : IDbConnectionFactory
    {
        public DbConnection CreateConnection()
        {
            using var escucha = new TcpListener(IPAddress.Loopback, 0);
            escucha.Start();
            var puerto = ((IPEndPoint)escucha.LocalEndpoint).Port;
            escucha.Stop();

            var conexion = new MySqlConnection(
                $"Server=127.0.0.1;Port={puerto};Database=x;User ID=x;Password=x;Connection Timeout=2");
            conexion.Open();
            return conexion;
        }
    }
}

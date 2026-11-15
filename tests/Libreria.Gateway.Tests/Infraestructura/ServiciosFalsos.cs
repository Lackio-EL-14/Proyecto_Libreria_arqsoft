using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Libreria.Gateway.Tests.Infraestructura;

/// <summary>
/// Levanta un servidor Kestrel real por microservicio (puerto libre) que responde con lo que recibió.
/// Así las pruebas comprueban a qué servicio llegó cada solicitud y que el gateway no la alteró.
/// </summary>
public sealed class ServiciosFalsos : IAsyncLifetime
{
    public static readonly IReadOnlyList<string> Clusters = ["clientes", "ventas", "productos", "usuarios"];

    private readonly List<WebApplication> _servidores = [];
    private readonly Dictionary<string, string> _direcciones = [];

    public IReadOnlyDictionary<string, string> Direcciones => _direcciones;

    public async Task InitializeAsync()
    {
        foreach (var cluster in Clusters)
        {
            var servidor = CrearServidor(cluster);
            await servidor.StartAsync();
            _servidores.Add(servidor);
            _direcciones[cluster] = servidor.Urls.Single();
        }
    }

    public async Task DisposeAsync()
    {
        foreach (var servidor in _servidores)
        {
            await servidor.DisposeAsync();
        }
    }

    private static WebApplication CrearServidor(string cluster)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        var servidor = builder.Build();
        servidor.Map("/{**ruta}", async (HttpContext contexto) =>
        {
            using var lector = new StreamReader(contexto.Request.Body);
            return Results.Json(new EcoDelServicio(
                cluster,
                contexto.Request.Method,
                contexto.Request.Path.Value ?? string.Empty,
                contexto.Request.QueryString.Value ?? string.Empty,
                contexto.Request.Headers.Authorization.ToString(),
                await lector.ReadToEndAsync()));
        });

        return servidor;
    }
}

public sealed record EcoDelServicio(
    string Servicio,
    string Metodo,
    string Ruta,
    string Query,
    string Authorization,
    string Cuerpo);

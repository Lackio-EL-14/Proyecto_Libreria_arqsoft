using System.Net;
using System.Text.Json;
using Libreria.Gateway.Tests.Infraestructura;

namespace Libreria.Gateway.Tests;

/// <summary>Si un microservicio no responde, el gateway devuelve 503 con ProblemDetails (sección 6.5).</summary>
public sealed class ServicioCaidoTests : IClassFixture<ServiciosFalsos>
{
    private readonly ServiciosFalsos _servicios;

    public ServicioCaidoTests(ServiciosFalsos servicios)
    {
        _servicios = servicios;
    }

    [Fact]
    public async Task Un_microservicio_caido_devuelve_503_con_ProblemDetails()
    {
        var destinos = new Dictionary<string, string>(_servicios.Direcciones)
        {
            ["clientes"] = GatewayFactory.DireccionSinServicio()
        };
        await using var gateway = new GatewayFactory(destinos);
        using var cliente = gateway.CreateClient();

        using var respuesta = await cliente.GetAsync("/api/clientes");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);

        using var problema = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        Assert.Equal(503, problema.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Servicio no disponible", problema.RootElement.GetProperty("title").GetString());
        Assert.Equal("clientes", problema.RootElement.GetProperty("servicio").GetString());
    }

    [Fact]
    public async Task Los_demas_microservicios_siguen_respondiendo_si_uno_cae()
    {
        var destinos = new Dictionary<string, string>(_servicios.Direcciones)
        {
            ["clientes"] = GatewayFactory.DireccionSinServicio()
        };
        await using var gateway = new GatewayFactory(destinos);
        using var cliente = gateway.CreateClient();

        using var respuesta = await cliente.GetAsync("/api/proveedores");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }
}

using Libreria.Gateway.Tests.Seguridad;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Libreria.Gateway.Tests.Infraestructura;

namespace Libreria.Gateway.Tests;

/// <summary>Cada ruta de la sección 6.2 llega a su microservicio sin alterarse (US-112).</summary>
public sealed class EnrutamientoTests : IClassFixture<ServiciosFalsos>, IAsyncLifetime
{
    private readonly GatewayFactory _gateway;
    private HttpClient _cliente = null!;

    public EnrutamientoTests(ServiciosFalsos servicios)
    {
        _gateway = new GatewayFactory(servicios.Direcciones);
    }

    public Task InitializeAsync()
    {
        _cliente = _gateway.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _cliente.Dispose();
        await _gateway.DisposeAsync();
    }

    [Theory]
    [InlineData("/api/clientes", "clientes")]
    [InlineData("/api/clientes/8f6c1a52-0000-4000-8000-000000000001", "clientes")]
    [InlineData("/api/proveedores", "proveedores")]
    [InlineData("/api/proveedores/8f6c1a52-0000-4000-8000-000000000004", "proveedores")]
    [InlineData("/api/productos", "productos")]
    [InlineData("/api/categorias/ubicaciones", "productos")]
    [InlineData("/api/marcas/paises", "productos")]
    [InlineData("/api/historico/8f6c1a52-0000-4000-8000-000000000002", "productos")]
    [InlineData("/api/usuarios", "usuarios")]
    [InlineData("/api/usuarios/nombres?ids=8f6c1a52-0000-4000-8000-000000000003", "usuarios")]
    public async Task La_ruta_llega_a_su_microservicio_con_ruta_y_query_intactas(string url, string servicioEsperado)
    {
        var eco = await _cliente.GetFromJsonAsync<EcoDelServicio>(url);

        Assert.NotNull(eco);
        Assert.Equal(servicioEsperado, eco.Servicio);
        Assert.Equal(url, eco.Ruta + eco.Query);
    }

    [Fact]
    public async Task Reenvia_metodo_cuerpo_y_token_Bearer_sin_cambios()
    {
        using var solicitud = new HttpRequestMessage(HttpMethod.Put, "/api/usuarios/contrasena")
        {
            Content = JsonContent.Create(new { contrasenaActual = "a", contrasenaNueva = "b" })
        };
        var token = TokensDePrueba.Vendedor();
        solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var respuesta = await _cliente.SendAsync(solicitud);
        var eco = await respuesta.Content.ReadFromJsonAsync<EcoDelServicio>();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(eco);
        Assert.Equal("usuarios", eco.Servicio);
        Assert.Equal("PUT", eco.Metodo);
        Assert.Equal($"Bearer {token}", eco.Authorization);
        Assert.Equal("""{"contrasenaActual":"a","contrasenaNueva":"b"}""", eco.Cuerpo);
    }

    [Theory]
    [InlineData("/api/reportes")]
    [InlineData("/api/auth/login")]
    [InlineData("/api/clientesx")]
    [InlineData("/otra/cosa")]
    public async Task Una_ruta_fuera_del_contrato_devuelve_404(string url)
    {
        using var respuesta = await _cliente.GetAsync(url);

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task La_salud_del_gateway_sigue_respondiendo()
    {
        using var respuesta = await _cliente.GetAsync("/salud");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }
}

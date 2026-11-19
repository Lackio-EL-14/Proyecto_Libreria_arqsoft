using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;

namespace Libreria.Servicio.Productos.Tests.Seguridad;

/// <summary>US-126: el MS Productos valida el JWT de Keycloak y solo atiende al Administrador.</summary>
public sealed class SeguridadJwtProductosTests : IClassFixture<SeguridadJwtProductosTests.Fabrica>
{
    private readonly HttpClient _http;

    public SeguridadJwtProductosTests(Fabrica fabrica)
    {
        _http = fabrica.CreateClient();
    }

    public static TheoryData<string, string?> TokensInvalidos() => new()
    {
        { "sin token", null },
        { "firma alterada", TokensDePrueba.FirmaAlterada() },
        { "firmado con otra clave", TokensDePrueba.FirmadoConOtraClave() },
        { "vencido", TokensDePrueba.Vencido() },
        { "audiencia equivocada", TokensDePrueba.AudienciaEquivocada() },
        { "emisor equivocado", TokensDePrueba.EmisorEquivocado() }
    };

    [Theory]
    [MemberData(nameof(TokensInvalidos))]
    public async Task TokenInvalido_Responde401(string caso, string? token)
    {
        using var respuesta = await GetAsync("api/categorias/ubicaciones", token);

        Assert.True(respuesta.StatusCode == HttpStatusCode.Unauthorized, caso);
    }

    [Theory]
    [InlineData("api/categorias/ubicaciones")]
    [InlineData("api/marcas/paises")]
    public async Task Administrador_200(string ruta)
    {
        using var respuesta = await GetAsync(ruta, TokensDePrueba.Admin());

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Theory]
    [InlineData("api/productos")]
    [InlineData("api/categorias/ubicaciones")]
    [InlineData("api/marcas/paises")]
    [InlineData("api/historico/d0000000-0000-0000-0000-000000000001")]
    public async Task Vendedor_Responde403_EnLosCuatroControladores(string ruta)
    {
        using var respuesta = await GetAsync(ruta, TokensDePrueba.Vendedor());

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task Salud_NoExigeToken()
    {
        using var respuesta = await GetAsync("salud", token: null);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task Swagger_PublicaElEsquemaBearer()
    {
        var documento = await _http.GetStringAsync("swagger/v1/swagger.json");

        Assert.Contains("\"bearer\"", documento);
    }

    private Task<HttpResponseMessage> GetAsync(string ruta, string? token)
    {
        var solicitud = new HttpRequestMessage(HttpMethod.Get, ruta);
        if (token is not null)
        {
            solicitud.Headers.Authorization = TokensDePrueba.Bearer(token);
        }

        return _http.SendAsync(solicitud);
    }

    /// <summary>MS Productos real; Mongo apunta a un puerto sin servidor (estas rutas no lo usan).</summary>
    public sealed class Fabrica : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:ProductosDb", "mongodb://127.0.0.1:1/libreria_productos_pruebas");
            builder.ConfigureTestServices(servicios => servicios.UsarJwtDePrueba());
        }
    }
}

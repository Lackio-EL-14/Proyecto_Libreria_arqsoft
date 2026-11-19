using System.Net;
using Libreria.Servicio.Proveedores.Application.Models;
using Libreria.Servicio.Proveedores.Application.Ports.Primary;
using Libreria.Servicio.Proveedores.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Libreria.Servicio.Proveedores.Tests.Seguridad;

/// <summary>US-126: el MS Proveedores valida el JWT de Keycloak y solo atiende al Administrador.</summary>
public sealed class SeguridadJwtProveedoresTests : IClassFixture<SeguridadJwtProveedoresTests.Fabrica>
{
    private readonly HttpClient _http;

    public SeguridadJwtProveedoresTests(Fabrica fabrica)
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
        using var respuesta = await GetAsync("api/proveedores", token);

        Assert.True(respuesta.StatusCode == HttpStatusCode.Unauthorized, caso);
    }

    [Fact]
    public async Task Administrador_Lista_200()
    {
        using var respuesta = await GetAsync("api/proveedores", TokensDePrueba.Admin());

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Theory]
    [InlineData("api/proveedores")]
    [InlineData("api/proveedores/f0000000-0000-0000-0000-000000000001")]
    public async Task Vendedor_Responde403(string ruta)
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

    /// <summary>MS Proveedores real (Program.cs, controlador y seguridad) con el servicio reemplazado.</summary>
    public sealed class Fabrica : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:ProveedoresDb", "Server=127.0.0.1,1;Database=x;TrustServerCertificate=True");
            builder.ConfigureTestServices(servicios =>
            {
                servicios.UsarJwtDePrueba();
                servicios.AddScoped<IProveedorService, ProveedorServiceVacio>();
            });
        }
    }

    private sealed class ProveedorServiceVacio : IProveedorService
    {
        public Task<IReadOnlyList<ProveedorDetalle>> ListarAsync(FiltroProveedores filtro) =>
            Task.FromResult<IReadOnlyList<ProveedorDetalle>>([]);

        public Task<Result<ProveedorDetalle>> ObtenerAsync(Guid publicId) => throw new NotSupportedException();
        public Task<Result<ProveedorDetalle>> RegistrarAsync(ProveedorInput input) => throw new NotSupportedException();
        public Task<Result<ProveedorDetalle>> ActualizarAsync(Guid publicId, ProveedorInput input) => throw new NotSupportedException();
        public Task<Result> CambiarEstadoAsync(Guid publicId, bool activo) => throw new NotSupportedException();
    }
}

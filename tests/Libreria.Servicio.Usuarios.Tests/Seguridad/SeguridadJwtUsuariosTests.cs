using System.Net;
using System.Net.Http.Json;
using Libreria.Servicio.Usuarios.Application.Ports.Secondary;
using Libreria.Servicio.Usuarios.Infrastructure.Identidad;
using Libreria.Servicio.Usuarios.Tests.TestDoubles;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Libreria.Servicio.Usuarios.Tests.Seguridad;

/// <summary>
/// US-126: el MS Usuarios valida el JWT de Keycloak, aplica los roles y toma al usuario actual
/// del token (no de cabeceras).
/// </summary>
public sealed class SeguridadJwtUsuariosTests : IClassFixture<SeguridadJwtUsuariosTests.Fabrica>
{
    private readonly Fabrica _fabrica;
    private readonly HttpClient _http;

    public SeguridadJwtUsuariosTests(Fabrica fabrica)
    {
        _fabrica = fabrica;
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
        using var respuesta = await EnviarAsync(HttpMethod.Get, "api/usuarios/roles", token);

        Assert.True(respuesta.StatusCode == HttpStatusCode.Unauthorized, caso);
    }

    [Fact]
    public async Task Administrador_VeLosRoles_200()
    {
        using var respuesta = await EnviarAsync(HttpMethod.Get, "api/usuarios/roles", TokensDePrueba.Admin());

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Theory]
    [InlineData("GET", "api/usuarios")]
    [InlineData("GET", "api/usuarios/roles")]
    [InlineData("POST", "api/usuarios")]
    [InlineData("DELETE", "api/usuarios/a0000000-0000-0000-0000-000000000001")]
    public async Task Vendedor_EnEndpointsDeAdministrador_403(string metodo, string ruta)
    {
        using var respuesta = await EnviarAsync(new HttpMethod(metodo), ruta, TokensDePrueba.Vendedor(),
            metodo == "POST" ? new { } : null);

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task Vendedor_ConsultaSuSesionActiva_200()
    {
        using var respuesta = await EnviarAsync(HttpMethod.Get, $"api/usuarios/{TokensDePrueba.SubVendedor}/sesion-activa", TokensDePrueba.Vendedor());

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task CabeceraXUsuarioFalsa_NoSuplantaAlAdmin_ElUsuarioSaleDelToken()
    {
        var identidad = (ProveedorIdentidadEnMemoria)_fabrica.Services.GetRequiredService<IProveedorIdentidad>();
        await identidad.FijarContrasenaAsync(ProveedorIdentidadEnMemoria.IdAdminSemilla, "Admin#2026xx", temporal: false);
        await identidad.FijarContrasenaAsync(ProveedorIdentidadEnMemoria.IdVendedorSemilla, "Vendedor#2026x", temporal: false);

        var solicitud = new HttpRequestMessage(HttpMethod.Put, "api/usuarios/contrasena")
        {
            Content = JsonContent.Create(new { currentPassword = "Vendedor#2026x", newPassword = "Nueva#2026xy", confirmation = "Nueva#2026xy" })
        };
        solicitud.Headers.Authorization = TokensDePrueba.Bearer(TokensDePrueba.Vendedor());
        solicitud.Headers.Add("X-Usuario-Id", TokensDePrueba.SubAdmin.ToString());
        solicitud.Headers.Add("X-Usuario-Nombre", "admin");

        using var respuesta = await _http.SendAsync(solicitud);

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.True((await identidad.VerificarContrasenaAsync("vendedor", "Nueva#2026xy")).IsSuccess);
        Assert.True((await identidad.VerificarContrasenaAsync("admin", "Admin#2026xx")).IsSuccess);
    }

    [Fact]
    public async Task Salud_NoExigeToken()
    {
        using var respuesta = await EnviarAsync(HttpMethod.Get, "salud", token: null);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task Swagger_PublicaElEsquemaBearer()
    {
        var documento = await _http.GetStringAsync("swagger/v1/swagger.json");

        Assert.Contains("\"bearer\"", documento);
    }

    private Task<HttpResponseMessage> EnviarAsync(HttpMethod metodo, string ruta, string? token, object? cuerpo = null)
    {
        var solicitud = new HttpRequestMessage(metodo, ruta);
        if (token is not null)
        {
            solicitud.Headers.Authorization = TokensDePrueba.Bearer(token);
        }

        if (cuerpo is not null)
        {
            solicitud.Content = JsonContent.Create(cuerpo);
        }

        return _http.SendAsync(solicitud);
    }

    /// <summary>MS Usuarios real con el repositorio falso y el proveedor de identidad en memoria.</summary>
    public sealed class Fabrica : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:UsuariosDb", "Server=127.0.0.1,1;Database=x;TrustServerCertificate=True");
            builder.UseSetting("ProveedorIdentidad:Modo", "EnMemoria");
            builder.ConfigureTestServices(servicios =>
            {
                servicios.UsarJwtDePrueba();
                var repositorio = new FakeUserRepository { IsActiveAdministrator = true, SessionActive = true };
                repositorio.ActiveRoles["Administrador"] = 1;
                repositorio.ActiveRoles["Vendedor"] = 2;
                servicios.AddScoped<IUserRepository>(_ => repositorio);
            });
        }
    }
}

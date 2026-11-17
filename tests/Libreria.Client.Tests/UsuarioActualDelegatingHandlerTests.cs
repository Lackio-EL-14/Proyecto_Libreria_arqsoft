using System.Net;
using System.Security.Claims;
using Libreria.Client.Clients.Common;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Libreria.Client.Tests;

public sealed class UsuarioActualDelegatingHandlerTests
{
    private const string PublicId = "a0000000-0000-0000-0000-000000000001";

    [Theory]
    [InlineData("/api/usuarios")]
    [InlineData("/api/usuarios?estado=Activo")]
    [InlineData("/api/usuarios/nombres?ids=a0000000-0000-0000-0000-000000000001")]
    public async Task SolicitudesDeUsuarios_PropaganPublicIdComoGuid(string ruta)
    {
        var capturador = await EnviarAsync(ruta);

        Assert.Equal(PublicId, capturador.UsuarioId);
        Assert.Equal("admin", capturador.UsuarioNombre);
    }

    [Theory]
    [InlineData("/api/clientes")]
    [InlineData("/api/proveedores")]
    [InlineData("/api/productos")]
    [InlineData("/api/auth/login")]
    public async Task OtrosServicios_ConservanElIdentificadorNumerico(string ruta)
    {
        var capturador = await EnviarAsync(ruta);

        Assert.Equal("1", capturador.UsuarioId);
        Assert.Equal("admin", capturador.UsuarioNombre);
    }

    [Fact]
    public async Task UsuariosSinPublicId_RechazaLaSolicitud()
    {
        var contexto = CrearContexto(incluirPublicId: false);
        using var cliente = new HttpClient(new UsuarioActualDelegatingHandler(
            new HttpContextAccessor { HttpContext = contexto })
        {
            InnerHandler = new CapturadorDeCabeceras()
        });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => cliente.GetAsync("http://localhost:5100/api/usuarios"));
    }

    private static async Task<CapturadorDeCabeceras> EnviarAsync(string ruta)
    {
        var capturador = new CapturadorDeCabeceras();
        using var cliente = new HttpClient(new UsuarioActualDelegatingHandler(
            new HttpContextAccessor { HttpContext = CrearContexto(incluirPublicId: true) })
        {
            InnerHandler = capturador
        })
        {
            BaseAddress = new Uri("http://localhost:5100")
        };

        using var respuesta = await cliente.GetAsync(ruta);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return capturador;
    }

    private static DefaultHttpContext CrearContexto(bool incluirPublicId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "1"),
            new(ClaimTypes.Name, "admin")
        };

        if (incluirPublicId)
        {
            claims.Add(new Claim("PublicId", PublicId));
        }

        return new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Cookie"))
        };
    }

    private sealed class CapturadorDeCabeceras : HttpMessageHandler
    {
        public string? UsuarioId { get; private set; }
        public string? UsuarioNombre { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            UsuarioId = request.Headers.GetValues("X-Usuario-Id").Single();
            UsuarioNombre = request.Headers.GetValues("X-Usuario-Nombre").Single();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}

using System.Globalization;
using System.Net;
using System.Security.Claims;
using Libreria.Client.Clients.Common;
using Libreria.Client.Seguridad;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Libreria.Client.Tests.Seguridad;

public sealed class BearerTokenDelegatingHandlerTests
{
    private const string Sub = "a0000000-0000-0000-0000-000000000001";
    private static readonly DateTimeOffset Ahora = new(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("/api/clientes")]
    [InlineData("/api/proveedores/f0000000-0000-0000-0000-000000000001")]
    [InlineData("/api/productos")]
    [InlineData("/api/usuarios")]
    public async Task SesionIniciada_AgregaElBearerATodasLasLlamadas(string ruta)
    {
        var (capturador, _) = await EnviarAsync(ruta, Autenticado("token-admin", Ahora.AddMinutes(30)));

        Assert.Equal("Bearer token-admin", capturador.Autorizacion);
    }

    [Theory]
    [InlineData("/api/clientes")]
    [InlineData("/api/productos")]
    public async Task OtrosServicios_NoRecibenCabecerasXUsuario(string ruta)
    {
        var (capturador, _) = await EnviarAsync(ruta, Autenticado("t", Ahora.AddMinutes(30)));

        Assert.Null(capturador.UsuarioId);
        Assert.Null(capturador.UsuarioNombre);
    }

    [Theory]
    [InlineData("/api/usuarios")]
    [InlineData("/api/usuarios/nombres?ids=a0000000-0000-0000-0000-000000000002")]
    public async Task Usuarios_RecibeTransitoriamenteElSubYElNombreDeUsuario(string ruta)
    {
        var (capturador, _) = await EnviarAsync(ruta, Autenticado("t", Ahora.AddMinutes(30)));

        Assert.Equal(Sub, capturador.UsuarioId);
        Assert.Equal("admin", capturador.UsuarioNombre);
    }

    [Fact]
    public async Task CabecerasXUsuarioFalsificadas_SeDescartan()
    {
        var (capturador, _) = await EnviarAsync("/api/usuarios", Anonimo(), solicitud =>
        {
            solicitud.Headers.Add(BearerTokenDelegatingHandler.UsuarioIdHeader, Guid.NewGuid().ToString());
            solicitud.Headers.Add(BearerTokenDelegatingHandler.UsuarioNombreHeader, "intruso");
        });

        Assert.Null(capturador.UsuarioId);
        Assert.Null(capturador.UsuarioNombre);
        Assert.Null(capturador.Autorizacion);
    }

    [Fact]
    public async Task TokenVencido_LanzaSesionExpirada_SinLlamarAlServicio()
    {
        var capturador = new Capturador();

        await Assert.ThrowsAsync<SesionExpiradaException>(() =>
            EnviarAsync("/api/clientes", Autenticado("t", Ahora.AddSeconds(-1)), capturador: capturador));

        Assert.Equal(0, capturador.Llamadas);
    }

    [Fact]
    public async Task Respuesta401_LanzaSesionExpirada()
    {
        var capturador = new Capturador { Estado = HttpStatusCode.Unauthorized };

        await Assert.ThrowsAsync<SesionExpiradaException>(() =>
            EnviarAsync("/api/productos", Autenticado("t", Ahora.AddMinutes(30)), capturador: capturador));
    }

    [Fact]
    public async Task SinSesion_NoAgregaToken_YEl401LlegaTalCual()
    {
        var capturador = new Capturador { Estado = HttpStatusCode.Unauthorized };

        var (_, respuesta) = await EnviarAsync("/api/clientes", Anonimo(), capturador: capturador);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Null(capturador.Autorizacion);
    }

    [Fact]
    public async Task DuranteLaValidacionDeLaCookie_UsaElTokenYElUsuarioEnCurso()
    {
        var contexto = Anonimo();
        contexto.Items[BearerTokenDelegatingHandler.ClaveTokenEnCurso] = "token-en-curso";
        contexto.Items[BearerTokenDelegatingHandler.ClaveVencimientoEnCurso] = Ahora.AddMinutes(5).ToString("o", CultureInfo.InvariantCulture);
        contexto.Items[BearerTokenDelegatingHandler.ClavePrincipalEnCurso] = Principal();

        var (capturador, _) = await EnviarAsync($"/api/usuarios/{Sub}/sesion-activa", contexto);

        Assert.Equal("Bearer token-en-curso", capturador.Autorizacion);
        Assert.Equal(Sub, capturador.UsuarioId);
    }

    private static async Task<(Capturador, HttpResponseMessage)> EnviarAsync(
        string ruta,
        HttpContext contexto,
        Action<HttpRequestMessage>? preparar = null,
        Capturador? capturador = null)
    {
        capturador ??= new Capturador();
        var handler = new BearerTokenDelegatingHandler(new HttpContextAccessor { HttpContext = contexto }, new TiempoFijo(Ahora))
        {
            InnerHandler = capturador
        };
        using var cliente = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5080") };
        using var solicitud = new HttpRequestMessage(HttpMethod.Get, ruta);
        preparar?.Invoke(solicitud);

        return (capturador, await cliente.SendAsync(solicitud));
    }

    private static ClaimsPrincipal Principal() => new(new ClaimsIdentity(
        [
            new Claim(ClaimsKeycloak.Sub, Sub),
            new Claim(ClaimsKeycloak.NombreUsuario, "admin"),
            new Claim(ClaimsKeycloak.Nombre, "Administrador Sistema"),
            new Claim(ClaimsKeycloak.Roles, "Administrador")
        ],
        CookieAuthenticationDefaults.AuthenticationScheme,
        ClaimsKeycloak.NombreUsuario,
        ClaimsKeycloak.Roles));

    private static HttpContext Autenticado(string token, DateTimeOffset vence)
    {
        var propiedades = new AuthenticationProperties();
        propiedades.StoreTokens(
        [
            new AuthenticationToken { Name = "access_token", Value = token },
            new AuthenticationToken { Name = "expires_at", Value = vence.ToString("o", CultureInfo.InvariantCulture) }
        ]);

        var principal = Principal();
        var servicios = new ServiceCollection()
            .AddSingleton<IAuthenticationService>(new AutenticacionFalsa(
                AuthenticateResult.Success(new AuthenticationTicket(principal, propiedades, CookieAuthenticationDefaults.AuthenticationScheme))))
            .BuildServiceProvider();

        return new DefaultHttpContext { User = principal, RequestServices = servicios };
    }

    private static HttpContext Anonimo() => new DefaultHttpContext();

    private sealed class Capturador : HttpMessageHandler
    {
        public HttpStatusCode Estado { get; init; } = HttpStatusCode.OK;
        public int Llamadas { get; private set; }
        public string? Autorizacion { get; private set; }
        public string? UsuarioId { get; private set; }
        public string? UsuarioNombre { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Llamadas++;
            Autorizacion = request.Headers.Authorization?.ToString();
            UsuarioId = request.Headers.TryGetValues(BearerTokenDelegatingHandler.UsuarioIdHeader, out var id) ? id.Single() : null;
            UsuarioNombre = request.Headers.TryGetValues(BearerTokenDelegatingHandler.UsuarioNombreHeader, out var nombre) ? nombre.Single() : null;
            return Task.FromResult(new HttpResponseMessage(Estado));
        }
    }

    private sealed class TiempoFijo(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
    }

    private sealed class AutenticacionFalsa(AuthenticateResult resultado) : IAuthenticationService
    {
        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme) => Task.FromResult(resultado);
        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties) => Task.CompletedTask;
        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
    }
}

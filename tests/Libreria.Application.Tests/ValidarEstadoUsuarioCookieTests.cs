using System.Security.Claims;
using System.Net;
using System.Net.Http.Json;
using Libreria.Client.Clients.Usuarios;
using Libreria.Client.Seguridad;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Libreria.Application.Tests;

public class ValidarEstadoUsuarioCookieTests
{
    [Fact]
    public async Task ValidatePrincipal_Activo_ConservaPrincipal()
    {
        var handler = new SesionActivaHttpMessageHandler(true);
        var servicio = CrearCliente(handler);
        var auth = new AuthenticationServiceFake();
        using var services = new ServiceCollection().AddSingleton<IAuthenticationService>(auth).BuildServiceProvider();
        var contexto = CrearContexto(services, "1");

        await CrearEvento(servicio).ValidatePrincipal(contexto);

        Assert.NotNull(contexto.Principal);
        Assert.Equal(0, auth.Cierres);
        Assert.Equal(1, handler.Consultas);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("abc")]
    [InlineData(null)]
    public async Task ValidatePrincipal_SesionInvalida_RechazaYEliminaCookie(string? identificador)
    {
        var servicio = CrearCliente(new SesionActivaHttpMessageHandler(false));
        var auth = new AuthenticationServiceFake();
        using var services = new ServiceCollection().AddSingleton<IAuthenticationService>(auth).BuildServiceProvider();
        var contexto = CrearContexto(services, identificador);

        await CrearEvento(servicio).ValidatePrincipal(contexto);

        Assert.Null(contexto.Principal);
        Assert.Equal(1, auth.Cierres);
    }

    private static ServicioUsuariosClient CrearCliente(HttpMessageHandler handler)
    {
        return new ServicioUsuariosClient(
            new HttpClient(handler)
            {
                BaseAddress = new Uri("http://usuarios.test/")
            });
    }

    private static ValidarEstadoUsuarioCookieEvents CrearEvento(
        ServicioUsuariosClient cliente)
    {
        return new ValidarEstadoUsuarioCookieEvents(
            cliente,
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(new SesionActivaOptions
            {
                ValidacionEstadoSegundos = 20
            }),
            NullLogger<ValidarEstadoUsuarioCookieEvents>.Instance);
    }

    private static CookieValidatePrincipalContext CrearContexto(IServiceProvider services, string? id)
    {
        var claims = id is null ? Array.Empty<Claim>() : new[] { new Claim(ClaimTypes.NameIdentifier, id) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Cookies"));
        var http = new DefaultHttpContext { RequestServices = services };
        var scheme = new AuthenticationScheme("Cookies", null, typeof(CookieAuthenticationHandler));
        return new CookieValidatePrincipalContext(http, scheme, new CookieAuthenticationOptions(),
            new AuthenticationTicket(principal, "Cookies"));
    }

    private sealed class AuthenticationServiceFake : IAuthenticationService
    {
        public int Cierres { get; private set; }
        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        {
            Cierres++;
            return Task.CompletedTask;
        }
        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
            => throw new NotSupportedException();
        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => throw new NotSupportedException();
        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => throw new NotSupportedException();
        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties)
            => throw new NotSupportedException();
    }

    private sealed class SesionActivaHttpMessageHandler : HttpMessageHandler
    {
        private readonly bool _activa;

        public SesionActivaHttpMessageHandler(bool activa)
        {
            _activa = activa;
        }

        public int Consultas { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Consultas++;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(_activa)
            });
        }
    }
}

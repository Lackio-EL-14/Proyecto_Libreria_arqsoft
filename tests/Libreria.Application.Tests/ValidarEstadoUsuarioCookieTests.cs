using System.Security.Claims;
using Libreria.Client.Seguridad;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Libreria.Application.Tests;

public class ValidarEstadoUsuarioCookieTests
{
    [Fact]
    public async Task ValidatePrincipal_Activo_ConservaPrincipal()
    {
        var servicio = new UsuarioSeguridadServiceFake();
        var auth = new AuthenticationServiceFake();
        using var services = new ServiceCollection().AddSingleton<IAuthenticationService>(auth).BuildServiceProvider();
        var contexto = CrearContexto(services, "1");

        await new ValidarEstadoUsuarioCookieEvents(servicio).ValidatePrincipal(contexto);

        Assert.NotNull(contexto.Principal);
        Assert.Equal(0, auth.Cierres);
        Assert.Equal(1, servicio.Consultas);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("abc")]
    [InlineData(null)]
    public async Task ValidatePrincipal_SesionInvalida_RechazaYEliminaCookie(string? identificador)
    {
        var servicio = new UsuarioSeguridadServiceFake { Activa = false };
        var auth = new AuthenticationServiceFake();
        using var services = new ServiceCollection().AddSingleton<IAuthenticationService>(auth).BuildServiceProvider();
        var contexto = CrearContexto(services, identificador);

        await new ValidarEstadoUsuarioCookieEvents(servicio).ValidatePrincipal(contexto);

        Assert.Null(contexto.Principal);
        Assert.Equal(1, auth.Cierres);
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
}

using System.Security.Claims;
using Libreria.Client.Seguridad;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Libreria.Client.Tests.Seguridad;

public sealed class SesionExpiradaFilterTests
{
    [Fact]
    public async Task Un401_CierraLaSesionLocal_YLlevaAlLoginConElMensaje()
    {
        var autenticacion = new AutenticacionEspia();
        var (ejecutando, ejecutado) = Contextos(autenticacion, new SesionExpiradaException("401"));

        await new SesionExpiradaFilter(NullLogger<SesionExpiradaFilter>.Instance)
            .OnPageHandlerExecutionAsync(ejecutando, () => Task.FromResult(ejecutado));

        Assert.True(ejecutado.ExceptionHandled);
        Assert.Equal("/Cuenta/Login?motivo=vencida", Assert.IsType<LocalRedirectResult>(ejecutado.Result).Url);
        Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, autenticacion.EsquemaCerrado);
    }

    [Fact]
    public async Task OtrasExcepciones_NoSeTocan()
    {
        var autenticacion = new AutenticacionEspia();
        var (ejecutando, ejecutado) = Contextos(autenticacion, new InvalidOperationException("otra"));

        await new SesionExpiradaFilter(NullLogger<SesionExpiradaFilter>.Instance)
            .OnPageHandlerExecutionAsync(ejecutando, () => Task.FromResult(ejecutado));

        Assert.False(ejecutado.ExceptionHandled);
        Assert.Null(autenticacion.EsquemaCerrado);
    }

    private static (PageHandlerExecutingContext, PageHandlerExecutedContext) Contextos(IAuthenticationService autenticacion, Exception excepcion)
    {
        var http = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddSingleton(autenticacion).BuildServiceProvider()
        };
        var pagina = new PageContext(new ActionContext(http, new RouteData(), new CompiledPageActionDescriptor()));
        var filtros = new List<IFilterMetadata>();
        var instancia = new object();

        return (
            new PageHandlerExecutingContext(pagina, filtros, null, new Dictionary<string, object?>(), instancia),
            new PageHandlerExecutedContext(pagina, filtros, null, instancia) { Exception = excepcion });
    }

    private sealed class AutenticacionEspia : IAuthenticationService
    {
        public string? EsquemaCerrado { get; private set; }

        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        {
            EsquemaCerrado = scheme;
            return Task.CompletedTask;
        }

        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme) => Task.FromResult(AuthenticateResult.NoResult());
        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties) => Task.CompletedTask;
    }
}

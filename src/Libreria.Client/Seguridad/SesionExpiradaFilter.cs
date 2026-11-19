using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Libreria.Client.Seguridad;

/// <summary>
/// Un 401 de cualquier servicio cierra la sesión local y lleva al login con el mensaje
/// «Su sesión venció…» (US-125). El usuario vuelve a autenticarse en Keycloak.
/// </summary>
public sealed class SesionExpiradaFilter : IAsyncPageFilter
{
    private readonly ILogger<SesionExpiradaFilter> _logger;

    public SesionExpiradaFilter(ILogger<SesionExpiradaFilter> logger)
    {
        _logger = logger;
    }

    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;

    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var resultado = await next();

        if (resultado.Exception is not SesionExpiradaException excepcion || resultado.ExceptionHandled)
        {
            return;
        }

        _logger.LogInformation("Se cierra la sesión: {Motivo}", excepcion.Message);
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        resultado.ExceptionHandled = true;
        resultado.Result = new LocalRedirectResult(MotivosSesion.UrlLogin(MotivosSesion.Vencida));
    }
}

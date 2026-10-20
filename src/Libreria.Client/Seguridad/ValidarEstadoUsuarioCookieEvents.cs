using System.Security.Claims;
using Libreria.Application.Ports.Primary;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Libreria.Client.Seguridad;

public class ValidarEstadoUsuarioCookieEvents : CookieAuthenticationEvents
{
    private readonly IUsuarioSeguridadService _seguridad;

    public ValidarEstadoUsuarioCookieEvents(IUsuarioSeguridadService seguridad)
    {
        _seguridad = seguridad;
    }

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var identificador = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        // Consultar la BD en cada validación; ninguna réplica conserva estados en memoria.
        if (!int.TryParse(identificador, out var usuarioId) ||
            !await _seguridad.SesionActivaAsync(usuarioId))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(context.Scheme.Name);
        }
    }
}

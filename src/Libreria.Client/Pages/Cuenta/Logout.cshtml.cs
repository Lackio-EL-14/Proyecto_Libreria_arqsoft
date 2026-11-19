using Libreria.Client.Seguridad;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Cuenta;

/// <summary>
/// Cierra la sesión en el Front (cookie) y en Keycloak (end_session con id_token_hint), y
/// vuelve a <c>/Cuenta/Login?motivo=cerrada</c> (US-125).
/// </summary>
[AllowAnonymous]
public class LogoutModel : PageModel
{
    // Solo por POST (con token antiforgery) para que un enlace externo
    // no pueda cerrar la sesión del usuario.
    public IActionResult OnGet() => RedirectToPage("/Index");

    public IActionResult OnPost()
    {
        var destino = MotivosSesion.UrlLogin(MotivosSesion.Cerrada);

        if (User.Identity?.IsAuthenticated != true)
        {
            return LocalRedirect(destino);
        }

        return SignOut(
            new AuthenticationProperties { RedirectUri = destino },
            CookieAuthenticationDefaults.AuthenticationScheme,
            OpenIdConnectDefaults.AuthenticationScheme);
    }
}

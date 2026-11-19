using Libreria.Client.Seguridad;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Libreria.Client.Pages.Cuenta;

/// <summary>
/// Entrada al login de Keycloak (US-125). Sin motivo, redirige directo a Keycloak; con un
/// motivo (sesión vencida, cerrada, cuenta inactiva…), muestra el mensaje y espera a que el
/// usuario pulse «Iniciar sesión», para no entrar en un bucle de redirecciones.
/// </summary>
[AllowAnonymous]
public class LoginModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Motivo { get; set; }

    public string? Mensaje { get; private set; }

    public bool EsAviso => Motivo is not (MotivosSesion.Cerrada or null);

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(DestinoSeguro());
        }

        Mensaje = MotivosSesion.Mensaje(Motivo);
        return Mensaje is null ? IniciarSesion(forzarCredenciales: false) : Page();
    }

    public IActionResult OnPost() => IniciarSesion(forzarCredenciales: true);

    private IActionResult IniciarSesion(bool forzarCredenciales)
    {
        var propiedades = new AuthenticationProperties { RedirectUri = DestinoSeguro() };
        if (forzarCredenciales)
        {
            // Tras un rechazo, Keycloak vuelve a pedir usuario y contraseña aunque su sesión SSO siga viva.
            propiedades.SetParameter(OpenIdConnectParameterNames.Prompt, "login");
        }

        return Challenge(propiedades, OpenIdConnectDefaults.AuthenticationScheme);
    }

    private string DestinoSeguro() =>
        !string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : "/";
}

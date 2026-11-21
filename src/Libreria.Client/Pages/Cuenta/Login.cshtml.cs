using Libreria.Client.Seguridad;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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
/// Si ya hay una sesión abierta (por ejemplo, el administrador abre en el mismo navegador el enlace
/// del correo de bienvenida), no se entra con ella: se muestra quién está conectado y se pide cerrar
/// esa sesión para ingresar con la otra cuenta y su contraseña temporal (US-131).
/// </summary>
[AllowAnonymous]
public class LoginModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Motivo { get; set; }

    public string? Mensaje { get; private set; }

    /// <summary>Nombre del usuario conectado cuando se llega al login con una sesión ya abierta.</summary>
    public string? SesionAbierta { get; private set; }

    public bool EsAviso => SesionAbierta is null
        && Motivo is not (MotivosSesion.Cerrada or MotivosSesion.PrimerIngreso or null);

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            SesionAbierta = User.NombreVisible();
            Mensaje = $"Hay una sesión abierta como {SesionAbierta}. Para ingresar con otra cuenta, por ejemplo la que recibió por correo, primero cierre esta sesión.";
            return Page();
        }

        Mensaje = MotivosSesion.Mensaje(Motivo);
        return Mensaje is null ? IniciarSesion(forzarCredenciales: false) : Page();
    }

    public IActionResult OnPost() => IniciarSesion(forzarCredenciales: true);

    /// <summary>Cierra la sesión abierta (Front y Keycloak) y vuelve al login para ingresar con otra cuenta.</summary>
    public IActionResult OnPostCambiarCuenta()
    {
        var destino = MotivosSesion.UrlLogin(
            Motivo == MotivosSesion.PrimerIngreso ? MotivosSesion.PrimerIngreso : MotivosSesion.Cerrada);

        if (User.Identity?.IsAuthenticated != true)
        {
            return LocalRedirect(destino);
        }

        return SignOut(
            new AuthenticationProperties { RedirectUri = destino },
            CookieAuthenticationDefaults.AuthenticationScheme,
            OpenIdConnectDefaults.AuthenticationScheme);
    }

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

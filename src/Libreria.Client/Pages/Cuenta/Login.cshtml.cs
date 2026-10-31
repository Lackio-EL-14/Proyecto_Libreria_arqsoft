using System.Security.Claims;
using System.Net;
using Libreria.Client.Clients.Common;
using Libreria.Client.Clients.Usuarios;
using Libreria.Client.DTOs.Usuarios;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Cuenta;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private const string MensajeCredencialesInvalidas =
        "Usuario o contraseña incorrectos.";

    private readonly IServicioUsuariosClient _usuariosClient;

    public LoginModel(IServicioUsuariosClient usuariosClient)
    {
        _usuariosClient = usuariosClient;
    }

    [BindProperty]
    public LoginInputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToPage("/Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            Input.Contrasena = string.Empty;
            return Page();
        }

        AuthenticatedUserDto? usuario;

        try
        {
            usuario = await _usuariosClient.IniciarSesionAsync(
                new LoginRequestDto(
                    Input.NombreUsuario,
                    Input.Contrasena),
                HttpContext.RequestAborted);
        }
        catch (ApiClientException exception)
            when (exception.StatusCode == HttpStatusCode.BadRequest)
        {
            ModelState.AddModelError(string.Empty, MensajeCredencialesInvalidas);
            Input.Contrasena = string.Empty;
            return Page();
        }

        if (usuario is null)
        {
            ModelState.AddModelError(string.Empty, MensajeCredencialesInvalidas);
            Input.Contrasena = string.Empty;
            return Page();
        }

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            CrearPrincipal(usuario));

        Input.Contrasena = string.Empty;

        if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
        {
            return LocalRedirect(ReturnUrl);
        }

        return RedirectToPage("/Index");
    }

    private static ClaimsPrincipal CrearPrincipal(AuthenticatedUserDto usuario)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.UserId.ToString()),
            new(ClaimTypes.Name, usuario.Username),
            new(ClaimTypes.GivenName, usuario.FullName),
            new(ClaimTypes.Role, usuario.Role),
            new("PublicId", usuario.PublicId.ToString())
        };

        var identidad = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        return new ClaimsPrincipal(identidad);
    }
}

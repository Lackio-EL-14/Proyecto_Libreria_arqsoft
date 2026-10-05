using System.Security.Claims;
using Libreria.Application.Models;
using Libreria.Application.Ports.Primary;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Cuenta;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly IAutenticacionService _autenticacionService;

    public LoginModel(IAutenticacionService autenticacionService)
    {
        _autenticacionService = autenticacionService;
    }

    [BindProperty]
    public LoginInput Input { get; set; } = new();

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
        var resultado = await _autenticacionService.AutenticarAsync(Input);

        if (!resultado.Exitoso || resultado.Usuario is null)
        {
            foreach (var error in resultado.Errores)
            {
                ModelState.AddModelError(error.Key, error.Value);
            }

            Input.Contrasena = string.Empty;
            return Page();
        }

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            CrearPrincipal(resultado.Usuario));

        if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
        {
            return LocalRedirect(ReturnUrl);
        }

        return RedirectToPage("/Index");
    }

    private static ClaimsPrincipal CrearPrincipal(UsuarioAutenticado usuario)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.UsuarioId.ToString()),
            new(ClaimTypes.Name, usuario.NombreUsuario),
            new(ClaimTypes.GivenName, usuario.NombreCompleto),
            new(ClaimTypes.Role, usuario.Rol)
        };

        var identidad = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        return new ClaimsPrincipal(identidad);
    }
}

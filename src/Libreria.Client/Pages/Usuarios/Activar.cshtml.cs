using Libreria.Application.Models;
using Libreria.Application.Ports.Primary;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Usuarios;

[AllowAnonymous]
public class ActivarModel : PageModel
{
    private readonly IUsuarioService _usuarioService;

    public ActivarModel(IUsuarioService usuarioService)
    {
        _usuarioService = usuarioService;
    }

    [BindProperty(SupportsGet = true, Name = "h")]
    public string TokenHash { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true, Name = "t")]
    public string Token { get; set; } = string.Empty;

    [BindProperty]
    public string Contrasena { get; set; } = string.Empty;

    [BindProperty]
    public string ConfirmarContrasena { get; set; } = string.Empty;

    public string? MensajeError { get; set; }

    public bool ActivacionExitosa { get; set; }

    public IActionResult OnGet()
    {
        if (string.IsNullOrWhiteSpace(TokenHash) ||
            string.IsNullOrWhiteSpace(Token))
        {
            MensajeError =
                "El enlace de activación no es válido o ha expirado.";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            var input = new UsuarioActivacionInput
            {
                TokenHash = TokenHash,
                Token = Token,
                Contrasena = Contrasena,
                ConfirmarContrasena = ConfirmarContrasena
            };

            await _usuarioService.ActivarAsync(input);

            ActivacionExitosa = true;
            return Page();
        }
        catch (ArgumentException error)
        {
            MensajeError = error.Message;
            return Page();
        }
        catch (InvalidOperationException)
        {
            MensajeError =
                "El enlace de activación no es válido o ha expirado.";

            return Page();
        }
    }
}
using Libreria.Client.Clients.Common;
using Libreria.Client.Clients.Usuarios;
using Libreria.Client.DTOs.Usuarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Usuarios;

[AllowAnonymous]
public class ActivarModel : PageModel
{
    private readonly IServicioUsuariosClient _usuariosClient;
    private readonly ILogger<ActivarModel> _logger;

    public ActivarModel(
        IServicioUsuariosClient usuariosClient,
        ILogger<ActivarModel> logger)
    {
        _usuariosClient = usuariosClient;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true, Name = "h")]
    public string TokenHash { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true, Name = "t")]
    public string Token { get; set; } = string.Empty;

    [BindProperty]
    public ActivateUserInputModel Input { get; set; } = new();

    public string? MensajeError { get; private set; }

    public bool ActivacionExitosa { get; private set; }

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
        if (string.IsNullOrWhiteSpace(TokenHash) ||
            string.IsNullOrWhiteSpace(Token))
        {
            MensajeError =
                "El enlace de activación no es válido o ha expirado.";

            return Page();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            await _usuariosClient.ActivarUsuarioAsync(
                new ActivateUserRequestDto(
                    TokenHash,
                    Token,
                    Input.Password,
                    Input.Confirmation));

            ActivacionExitosa = true;
            return Page();
        }
        catch (ApiClientException error)
        {
            MensajeError = error.Message;
            return Page();
        }
        catch (ServicioNoDisponibleException error)
        {
            _logger.LogError(
                error,
                "El servicio de Usuarios no está disponible durante la activación.");

            MensajeError =
                "No fue posible activar la cuenta en este momento. Intente nuevamente.";

            return Page();
        }
    }
}

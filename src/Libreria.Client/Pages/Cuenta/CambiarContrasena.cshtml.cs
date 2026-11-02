using Libreria.Application.Models;
using Libreria.Client.Clients.Common;
using Libreria.Client.Clients.Usuarios;
using Libreria.Client.DTOs.Usuarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Cuenta;

[Authorize]
public class CambiarContrasenaModel : PageModel
{
    private readonly IServicioUsuariosClient _usuariosClient;

    public CambiarContrasenaModel(
        IServicioUsuariosClient usuariosClient)
    {
        _usuariosClient = usuariosClient;
    }

    [BindProperty]
    public CambioContrasenaInput Input { get; set; } = new();

    public async Task<IActionResult> OnPostAsync()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            LimpiarContrasenas();
            ModelState.AddModelError(
                string.Empty,
                "Revise los datos ingresados.");
            return Page();
        }

        try
        {
            await _usuariosClient.CambiarContrasenaAsync(
                new ChangePasswordRequestDto(
                    Input.ContrasenaActual,
                    Input.NuevaContrasena,
                    Input.Confirmacion),
                HttpContext.RequestAborted);

            TempData["MensajeExito"] =
                "Contraseña actualizada correctamente.";

            return RedirectToPage();
        }
        catch (ApiClientException exception)
        {
            LimpiarContrasenas();

            ModelState.AddModelError(
                string.Empty,
                exception.Message);

            return Page();
        }
    }

    private void LimpiarContrasenas()
    {
        Input = new CambioContrasenaInput();
        ModelState.Clear();
    }
}

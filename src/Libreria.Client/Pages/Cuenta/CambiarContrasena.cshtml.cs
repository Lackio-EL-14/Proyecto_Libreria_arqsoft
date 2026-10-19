using Libreria.Application.Models;
using Libreria.Application.Ports.Primary;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Cuenta;

[Authorize]
public class CambiarContrasenaModel : PageModel
{
    private readonly IUsuarioService _usuarios;

    public CambiarContrasenaModel(IUsuarioService usuarios)
    {
        _usuarios = usuarios;
    }

    [BindProperty]
    public CambioContrasenaInput Input { get; set; } = new();

    public async Task<IActionResult> OnPostAsync()
    {
        if (User.Identity?.IsAuthenticated != true)
            return Challenge();

        var resultado = ModelState.IsValid
            ? await _usuarios.CambiarContrasenaAsync(Input)
            : null;

        // No devolver valores de contraseñas al renderizar la respuesta.
        Input = new CambioContrasenaInput();
        ModelState.Clear();

        if (resultado?.Exitoso == true)
        {
            TempData["MensajeExito"] = "Contraseña actualizada correctamente.";
            return RedirectToPage();
        }

        if (resultado is null)
            ModelState.AddModelError(string.Empty, "Revise los datos ingresados.");
        else
            foreach (var error in resultado.Errores)
                ModelState.AddModelError(error.Key, error.Value);

        return Page();
    }
}

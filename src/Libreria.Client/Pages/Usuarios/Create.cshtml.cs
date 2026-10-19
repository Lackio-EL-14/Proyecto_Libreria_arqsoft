using Libreria.Application.Ports.Primary;
using Libreria.Client.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Usuarios;

[Authorize(Policy = PoliticasAcceso.SoloAdministrador)]
public class CreateModel : PageModel
{
    private readonly IGeneradorNombreUsuario _generadorNombreUsuario;

    public CreateModel(IGeneradorNombreUsuario generadorNombreUsuario)
    {
        _generadorNombreUsuario = generadorNombreUsuario;
    }

    [BindProperty]
    public string Nombre { get; set; } = string.Empty;

    [BindProperty]
    public string Apellido { get; set; } = string.Empty;

    public string NombreUsuario { get; private set; } = string.Empty;

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostGenerarAsync()
    {
        if (string.IsNullOrWhiteSpace(Nombre))
            ModelState.AddModelError(nameof(Nombre), "El nombre es obligatorio.");

        if (string.IsNullOrWhiteSpace(Apellido))
            ModelState.AddModelError(nameof(Apellido), "El apellido es obligatorio.");

        if (!ModelState.IsValid)
            return Page();

        NombreUsuario =
            await _generadorNombreUsuario.GenerarAsync(Nombre, Apellido);

        return Page();
    }
}
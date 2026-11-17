using Libreria.Client.Clients.Proveedores;
using Libreria.Client.DTOs.Proveedores;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Proveedores;

public sealed class CreateModel : PageModel
{
    private readonly IServicioProveedoresClient _proveedores;

    public CreateModel(IServicioProveedoresClient proveedores)
    {
        _proveedores = proveedores;
    }

    [BindProperty]
    public ProveedorInput Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var resultado = await _proveedores.RegistrarProveedorAsync(Input);
        if (!resultado.Exitoso)
        {
            ModelState.AgregarErrores(resultado.Errores);
            return Page();
        }

        TempData["MensajeExito"] = "Proveedor registrado correctamente.";
        return RedirectToPage("./Index");
    }
}

using Libreria.Client.Adapters.Proveedores;
using Libreria.Client.DTOs.Proveedores;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Proveedores;

public sealed class CreateModel : PageModel
{
    private readonly IProveedoresApi _proveedores;

    public CreateModel(IProveedoresApi proveedores)
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
        // Primero los mensajes del formulario (los mismos del MS); lo que pasa, lo valida el MS (Result).
        if (!ModelState.IsValid)
        {
            return Page();
        }

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

using Libreria.Client.Adapters.Productos;
using Libreria.Client.DTOs.Common;
using Libreria.Client.DTOs.Productos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Marcas;

public class CreateModel : PageModel
{
    private readonly IProductosApi _productos;

    public CreateModel(IProductosApi productos)
    {
        _productos = productos;
    }

    [BindProperty]
    public MarcaInput Input { get; set; } = new();

    public IReadOnlyList<string> Paises { get; private set; } = [];

    public async Task OnGetAsync()
    {
        await CargarPaisesAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var resultado = await _productos.CrearMarcaAsync(Input);
        if (!resultado.Exitoso)
        {
            AgregarErrores(resultado.Errores);
            await CargarPaisesAsync();
            return Page();
        }

        TempData["MensajeExito"] = "Marca registrada correctamente.";
        return RedirectToPage("./Index");
    }

    private async Task CargarPaisesAsync()
    {
        Paises = await _productos.ObtenerPaisesMarcaAsync();
    }

    private void AgregarErrores(IReadOnlyDictionary<string, string> errores)
    {
        foreach (var error in errores)
        {
            ModelState.AddModelError(error.Key, error.Value);
        }
    }
}

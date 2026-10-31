using Libreria.Client.Clients;
using Libreria.Client.DTOs.Ventas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Categorias;

public class CreateModel : PageModel
{
    private readonly IVentasApiClient _ventas;

    public CreateModel(IVentasApiClient ventas)
    {
        _ventas = ventas;
    }

    [BindProperty]
    public CategoriaInput Input { get; set; } = new();

    public IReadOnlyList<string> Ubicaciones { get; private set; } = [];

    public async Task OnGetAsync()
    {
        await CargarUbicacionesAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var resultado = await _ventas.CrearCategoriaAsync(Input);
        if (!resultado.Exitoso)
        {
            AgregarErrores(resultado.Errores);
            await CargarUbicacionesAsync();
            return Page();
        }

        TempData["MensajeExito"] = "Categoría registrada exitosamente.";
        return RedirectToPage("./Index");
    }

    private async Task CargarUbicacionesAsync()
    {
        Ubicaciones = await _ventas.ObtenerUbicacionesCategoriaAsync();
    }

    private void AgregarErrores(IReadOnlyDictionary<string, string> errores)
    {
        foreach (var error in errores)
        {
            ModelState.AddModelError(error.Key, error.Value);
        }
    }
}

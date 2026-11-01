using Libreria.Client.Clients;
using Libreria.Client.DTOs.Ventas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Marcas;

public class CreateModel : PageModel
{
    private readonly IVentasApiClient _ventas;

    public CreateModel(IVentasApiClient ventas)
    {
        _ventas = ventas;
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
        var resultado = await _ventas.CrearMarcaAsync(Input);
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
        Paises = await _ventas.ObtenerPaisesMarcaAsync();
    }

    private void AgregarErrores(IReadOnlyDictionary<string, string> errores)
    {
        foreach (var error in errores)
        {
            ModelState.AddModelError(error.Key, error.Value);
        }
    }
}

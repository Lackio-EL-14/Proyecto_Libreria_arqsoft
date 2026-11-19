using Libreria.Client.Adapters.Productos;
using Libreria.Client.DTOs.Common;
using Libreria.Client.DTOs.Productos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Marcas;

public class ReactivarModel : PageModel
{
    private readonly IProductosApi _productos;

    public ReactivarModel(IProductosApi productos)
    {
        _productos = productos;
    }

    [BindProperty]
    public Guid PublicId { get; set; }

    public MarcaDto? Marca { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        Marca = await _productos.ObtenerMarcaAsync(id, false);
        if (Marca is null)
        {
            return NotFound();
        }

        PublicId = Marca.PublicId;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var resultado = await _productos.CambiarEstadoMarcaAsync(PublicId, true);
        if (!resultado.Exitoso)
        {
            return NotFound();
        }

        TempData["MensajeExito"] = "Marca reactivada correctamente.";
        return RedirectToPage("./Index");
    }
}

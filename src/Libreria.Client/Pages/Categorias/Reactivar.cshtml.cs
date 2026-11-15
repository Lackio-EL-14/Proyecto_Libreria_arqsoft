using Libreria.Client.Clients.Productos;
using Libreria.Client.DTOs.Common;
using Libreria.Client.DTOs.Productos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Categorias;

public class ReactivarModel : PageModel
{
    private readonly IServicioProductosClient _productos;

    public ReactivarModel(IServicioProductosClient productos)
    {
        _productos = productos;
    }

    [BindProperty]
    public Guid PublicId { get; set; }

    public CategoriaDto? Categoria { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        Categoria = await _productos.ObtenerCategoriaAsync(id, false);
        if (Categoria is null)
        {
            return NotFound();
        }

        PublicId = Categoria.PublicId;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var resultado = await _productos.CambiarEstadoCategoriaAsync(PublicId, true);
        if (!resultado.Exitoso)
        {
            return NotFound();
        }

        TempData["MensajeExito"] = "Categoría reactivada correctamente.";
        return RedirectToPage("./Index");
    }
}

using Libreria.Client.Adapters.Productos;
using Libreria.Client.DTOs.Common;
using Libreria.Client.DTOs.Productos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Categorias;

public class IndexModel : PageModel
{
    private readonly IProductosApi _productos;

    public IndexModel(IProductosApi productos)
    {
        _productos = productos;
    }

    public IReadOnlyList<CategoriaDto> Categorias { get; private set; } = [];
    public IReadOnlySet<Guid> CategoriasConProductosActivos { get; private set; } = new HashSet<Guid>();

    [BindProperty(SupportsGet = true)]
    public string? Busqueda { get; set; }

    public async Task OnGetAsync()
    {
        Categorias = await _productos.ObtenerCategoriasAsync(Busqueda);

        CategoriasConProductosActivos = Categorias
            .Where(categoria => categoria.TieneProductosActivos)
            .Select(categoria => categoria.PublicId)
            .ToHashSet();
    }

    public async Task<IActionResult> OnPostDarDeBajaAsync(Guid publicId)
    {
        var resultado = await _productos.CambiarEstadoCategoriaAsync(publicId, false);
        if (!resultado.Exitoso)
        {
            return NotFound();
        }

        TempData["MensajeExito"] = "Categoría dada de baja correctamente.";
        return RedirectToPage("./Index");
    }
}

using Libreria.Client.Clients;
using Libreria.Client.DTOs.Ventas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Categorias;

public class IndexModel : PageModel
{
    private readonly IVentasApiClient _ventas;

    public IndexModel(IVentasApiClient ventas)
    {
        _ventas = ventas;
    }

    public IReadOnlyList<CategoriaDto> Categorias { get; private set; } = [];
    public IReadOnlySet<Guid> CategoriasConProductosActivos { get; private set; } = new HashSet<Guid>();

    [BindProperty(SupportsGet = true)]
    public string? Busqueda { get; set; }

    public async Task OnGetAsync()
    {
        Categorias = await _ventas.ObtenerCategoriasAsync(Busqueda);

        CategoriasConProductosActivos = Categorias
            .Where(categoria => categoria.TieneProductosActivos)
            .Select(categoria => categoria.PublicId)
            .ToHashSet();
    }

    public async Task<IActionResult> OnPostDarDeBajaAsync(Guid publicId)
    {
        var resultado = await _ventas.CambiarEstadoCategoriaAsync(publicId, false);
        if (!resultado.Exitoso)
        {
            return NotFound();
        }

        TempData["MensajeExito"] = "Categoría dada de baja correctamente.";
        return RedirectToPage("./Index");
    }
}

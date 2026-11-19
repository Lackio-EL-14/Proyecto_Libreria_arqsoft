using Libreria.Client.Adapters.Productos;
using Libreria.Client.DTOs.Common;
using Libreria.Client.DTOs.Productos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Marcas;

public class IndexModel : PageModel
{
    private readonly IProductosApi _productos;

    public IndexModel(IProductosApi productos)
    {
        _productos = productos;
    }

    public IReadOnlyList<MarcaDto> Marcas { get; private set; } = [];
    public IReadOnlySet<Guid> MarcasConProductosActivos { get; private set; } = new HashSet<Guid>();
    public string? NombreBusqueda { get; private set; }

    public async Task OnGetAsync(string? nombre)
    {
        NombreBusqueda = nombre?.Trim();
        Marcas = await _productos.ObtenerMarcasAsync(NombreBusqueda);

        MarcasConProductosActivos = Marcas
            .Where(marca => marca.TieneProductosActivos)
            .Select(marca => marca.PublicId)
            .ToHashSet();
    }

    public async Task<IActionResult> OnPostDarDeBajaAsync(Guid publicId)
    {
        var resultado = await _productos.CambiarEstadoMarcaAsync(publicId, false);
        if (!resultado.Exitoso)
        {
            return NotFound();
        }

        TempData["MensajeExito"] = "Marca dada de baja correctamente.";
        return RedirectToPage("./Index");
    }
}

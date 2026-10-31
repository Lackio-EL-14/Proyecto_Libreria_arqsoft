using Libreria.Client.Clients;
using Libreria.Client.DTOs.Ventas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Marcas;

public class IndexModel : PageModel
{
    private readonly IVentasApiClient _ventas;

    public IndexModel(IVentasApiClient ventas)
    {
        _ventas = ventas;
    }

    public IReadOnlyList<MarcaDto> Marcas { get; private set; } = [];
    public IReadOnlySet<Guid> MarcasConProductosActivos { get; private set; } = new HashSet<Guid>();
    public string? NombreBusqueda { get; private set; }

    public async Task OnGetAsync(string? nombre)
    {
        NombreBusqueda = nombre?.Trim();
        Marcas = await _ventas.ObtenerMarcasAsync(NombreBusqueda);

        MarcasConProductosActivos = Marcas
            .Where(marca => marca.TieneProductosActivos)
            .Select(marca => marca.PublicId)
            .ToHashSet();
    }

    public async Task<IActionResult> OnPostDarDeBajaAsync(Guid publicId)
    {
        var resultado = await _ventas.CambiarEstadoMarcaAsync(publicId, false);
        if (!resultado.Exitoso)
        {
            return NotFound();
        }

        TempData["MensajeExito"] = "Marca dada de baja correctamente.";
        return RedirectToPage("./Index");
    }
}

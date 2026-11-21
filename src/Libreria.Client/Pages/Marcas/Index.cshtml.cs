using Libreria.Client.Adapters.Productos;
using Libreria.Client.DTOs.Productos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Marcas;

/// <summary>
/// Consulta de marcas. Consigna del 17/11: un solo CRUD por microservicio (Producto);
/// categorías y marcas son catálogos de apoyo sin alta, edición ni baja en el Front.
/// </summary>
public class IndexModel : PageModel
{
    private readonly IProductosApi _productos;

    public IndexModel(IProductosApi productos)
    {
        _productos = productos;
    }

    public IReadOnlyList<MarcaDto> Marcas { get; private set; } = [];
    public string? NombreBusqueda { get; private set; }

    public async Task OnGetAsync(string? nombre)
    {
        NombreBusqueda = nombre?.Trim();
        Marcas = await _productos.ObtenerMarcasAsync(NombreBusqueda);
    }
}

using Libreria.Client.Adapters.Productos;
using Libreria.Client.DTOs.Productos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Categorias;

/// <summary>
/// Consulta de categorías. Consigna del 17/11: un solo CRUD por microservicio (Producto);
/// categorías y marcas son catálogos de apoyo sin alta, edición ni baja en el Front.
/// </summary>
public class IndexModel : PageModel
{
    private readonly IProductosApi _productos;

    public IndexModel(IProductosApi productos)
    {
        _productos = productos;
    }

    public IReadOnlyList<CategoriaDto> Categorias { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? Busqueda { get; set; }

    public async Task OnGetAsync()
    {
        Categorias = await _productos.ObtenerCategoriasAsync(Busqueda);
    }
}

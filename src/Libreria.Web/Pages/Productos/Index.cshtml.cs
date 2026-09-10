using Libreria.Web.Pages.Productos.Models;
using Libreria.Web.Pages.Productos.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Web.Pages.Productos;

public class IndexModel : PageModel
{
    private readonly IProductoService _service;

    public IndexModel(IProductoService service)
    {
        _service = service;
    }

    public IReadOnlyList<ProductoListItem> Productos { get; private set; } = [];
    public IReadOnlyList<CategoriaOption> Categorias { get; private set; } = [];
    public IReadOnlyList<MarcaOption> Marcas { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? Busqueda { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? CategoriaId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? MarcaId { get; set; }

    public void OnGet()
    {
        var listado = _service.ObtenerListado(Busqueda, CategoriaId, MarcaId);
        Productos = listado.Productos;
        Categorias = listado.Categorias;
        Marcas = listado.Marcas;
    }
}

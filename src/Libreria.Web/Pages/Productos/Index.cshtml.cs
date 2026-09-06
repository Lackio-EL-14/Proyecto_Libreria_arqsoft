using Libreria.Web.Pages.Productos.Models;
using Libreria.Web.Pages.Productos.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Web.Pages.Productos;

public class IndexModel : PageModel
{
    private readonly IProductoRepository _repository;

    public IndexModel(IProductoRepository repository)
    {
        _repository = repository;
    }

    public List<ProductoListItem> Productos { get; private set; } = new();
    public List<CategoriaFiltroItem> Categorias { get; private set; } = new();
    public List<MarcaFiltroItem> Marcas { get; private set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Busqueda { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? CategoriaId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? MarcaId { get; set; }

    public void OnGet()
    {
        Categorias = _repository.ObtenerCategoriasActivas();
        Marcas = _repository.ObtenerMarcasActivas();

        Productos = _repository.ObtenerProductos(
            Busqueda,
            CategoriaId,
            MarcaId
        );
    }
}
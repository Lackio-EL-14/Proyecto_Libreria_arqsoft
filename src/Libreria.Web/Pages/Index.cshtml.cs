using System;
using System.Linq;
using System.Threading.Tasks;
using Libreria.Web.Data.Factories;
using Libreria.Web.Data.Repositories;
using Libreria.Web.Domain.Entities;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Web.Pages;

public class IndexModel : PageModel
{
    private readonly ICrudRepository<Producto> _productoRepo;
    private readonly ICrudRepository<Categoria> _categoriaRepo;
    private readonly ICrudRepository<Marca> _marcaRepo;

    public IndexModel(
        CrudRepositoryFactory<Producto> productoFactory,
        CrudRepositoryFactory<Categoria> categoriaFactory,
        CrudRepositoryFactory<Marca> marcaFactory)
    {
        _productoRepo = productoFactory.CrearRepositorio();
        _categoriaRepo = categoriaFactory.CrearRepositorio();
        _marcaRepo = marcaFactory.CrearRepositorio();
    }

    public int TotalProductos { get; private set; }
    public int ProductosStockCritico { get; private set; }
    public int TotalCategorias { get; private set; }
    public int TotalMarcas { get; private set; }
    public int TotalPerecederos { get; private set; }

    public async Task OnGetAsync()
    {
        try
        {
            var productos = await _productoRepo.ObtenerActivasAsync();
            var categorias = await _categoriaRepo.ObtenerActivasAsync();
            var marcas = await _marcaRepo.ObtenerActivasAsync();

            TotalProductos = productos.Count;
            ProductosStockCritico = productos.Count(p => p.Stock <= 5);
            TotalPerecederos = productos.Count(p => p.EsPerecedero);
            TotalCategorias = categorias.Count;
            TotalMarcas = marcas.Count;
        }
        catch
        {
            TotalProductos = 0;
            ProductosStockCritico = 0;
            TotalPerecederos = 0;
            TotalCategorias = 0;
            TotalMarcas = 0;
        }
    }
}

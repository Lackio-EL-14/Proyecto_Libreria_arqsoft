using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Libreria.Web.Data.Factories;
using Libreria.Web.Data.Repositories;
using Libreria.Web.Domain.Entities;
using Libreria.Web.Pages.Productos.Models;
using Libreria.Web.Pages.Productos.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Web.Pages.Productos;

public class IndexModel : PageModel
{
    private readonly IProductoService _service;
    private readonly ICrudRepository<Producto> _repository;

    public IndexModel(
        IProductoService service,
        CrudRepositoryFactory<Producto> factory)
    {
        _service = service;
        _repository = factory.CrearRepositorio();
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

    public async Task OnGetAsync()
    {
        var listado = await _service.ObtenerListadoAsync(
            Busqueda,
            CategoriaId,
            MarcaId,
            _repository);

        Productos = listado.Productos;
        Categorias = listado.Categorias;
        Marcas = listado.Marcas;
    }

    public async Task<IActionResult> OnPostDarDeBajaAsync(Guid publicId)
    {
        if (!await _service.DarDeBajaAsync(publicId, _repository))
        {
            return NotFound();
        }

        TempData["MensajeExito"] = "Producto dado de baja correctamente.";
        return RedirectToPage("./Index");
    }
}

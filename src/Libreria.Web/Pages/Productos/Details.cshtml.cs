using Libreria.Web.Pages.Productos.Models;
using Libreria.Web.Pages.Productos.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Libreria.Web.Data.Factories;
using Libreria.Web.Data.Repositories;
using Libreria.Web.Domain.Entities;

namespace Libreria.Web.Pages.Productos;

public class DetailsModel : PageModel
{
    public Guid PublicId { get; private set; }

    private readonly IProductoService _service;
    private readonly ICrudRepository<Producto> _repository;

    public DetailsModel(
        IProductoService service,
        CrudRepositoryFactory<Producto> factory)
    {
        _service = service;
        _repository = factory.CrearRepositorio();
    }

    public ProductoDetalle Producto { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var producto = await _service.ObtenerDetalleAsync(id, _repository);

        if (producto is null)
        {
            return NotFound();
        }

        PublicId = id;
        Producto = producto;
        return Page();
    }
}

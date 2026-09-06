using Libreria.Web.Pages.Productos.Models;
using Libreria.Web.Pages.Productos.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Web.Pages.Productos;

public class DetailsModel : PageModel
{
    private readonly IProductoRepository _repository;

    public DetailsModel(IProductoRepository repository)
    {
        _repository = repository;
    }

    public ProductoDetalle Producto { get; private set; } = null!;

    public IActionResult OnGet(int id)
    {
        var producto = _repository.ObtenerProductoPorId(id);

        if (producto is null)
        {
            return NotFound();
        }

        Producto = producto;

        return Page();
    }
}

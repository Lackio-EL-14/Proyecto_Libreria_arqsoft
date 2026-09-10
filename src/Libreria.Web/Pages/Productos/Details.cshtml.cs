using Libreria.Web.Pages.Productos.Models;
using Libreria.Web.Pages.Productos.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Web.Pages.Productos;

public class DetailsModel : PageModel
{
    private readonly IProductoService _service;

    public DetailsModel(IProductoService service)
    {
        _service = service;
    }

    public ProductoDetalle Producto { get; private set; } = null!;

    public IActionResult OnGet(int id)
    {
        var producto = _service.ObtenerDetalle(id);
        if (producto is null)
        {
            return NotFound();
        }

        Producto = producto;
        return Page();
    }
}

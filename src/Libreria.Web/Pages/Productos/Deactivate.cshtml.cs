using Libreria.Web.Pages.Productos.Models;
using Libreria.Web.Pages.Productos.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Web.Pages.Productos;

public class DeactivateModel : PageModel
{
    private readonly IProductoRepository _repository;

    public DeactivateModel(IProductoRepository repository)
    {
        _repository = repository;
    }

    public ProductoDetalle Producto { get; private set; } = null!;

    [BindProperty]
    public int ProductoId { get; set; }

    public IActionResult OnGet(int id)
    {
        var producto = _repository.ObtenerProductoPorId(id);

        if (producto is null)
        {
            return NotFound();
        }

        Producto = producto;
        ProductoId = producto.ProductoId;

        return Page();
    }

    public IActionResult OnPost()
    {
        if (!_repository.DarDeBaja(ProductoId))
        {
            return NotFound();
        }

        TempData["MensajeExito"] = "Producto dado de baja correctamente.";

        return RedirectToPage("Index");
    }
}

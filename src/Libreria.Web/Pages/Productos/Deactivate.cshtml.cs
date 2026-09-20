using Libreria.Web.Pages.Productos.Models;
using Libreria.Web.Pages.Productos.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Libreria.Web.Data.Factories;
using Libreria.Web.Data.Repositories;
using Libreria.Web.Domain.Entities;


namespace Libreria.Web.Pages.Productos;

public class DeactivateModel : PageModel
{
    private readonly IProductoService _service;
    private readonly ICrudRepository<Producto> _repository;

    public DeactivateModel(
        IProductoService service,
        CrudRepositoryFactory<Producto> factory)
    {
        _service = service;
        _repository = factory.CrearRepositorio();
    }

    public ProductoDetalle Producto { get; private set; } = null!;

    public Guid PublicId { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var producto = await _service.ObtenerParaBajaAsync(id, _repository);


        if (producto is null)
        {
            return NotFound();
        }

        PublicId = id;
        Producto = producto;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        if (!await _service.DarDeBajaAsync(id, _repository))
        {
            return NotFound();
        }

        TempData["MensajeExito"] = "Producto dado de baja correctamente.";
        return RedirectToPage("Index");
    }
}
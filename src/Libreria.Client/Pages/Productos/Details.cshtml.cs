using Libreria.Application.Domain;
using Libreria.Application.Models;
using Libreria.Application.Ports.Secondary;
using Libreria.Application.Factories;
using Libreria.Application.Services;
using Libreria.Application.Ports.Primary;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;


namespace Libreria.Client.Pages.Productos;

public class DetailsModel : PageModel
{
    public Guid PublicId { get; private set; }

    private readonly IProductoService _service;
    private readonly ICrudRepository<Producto> _repository;
    private readonly IAuditoriaService _auditoriaService;

    public DetailsModel(
        IProductoService service,
        CrudRepositoryFactory<Producto> factory,
        IAuditoriaService auditoriaService)
    {
        _service = service;
        _auditoriaService = auditoriaService;
        _repository = factory.CrearRepositorio();
    }

    public ProductoDetalle Producto { get; private set; } = null!;
    public AuditoriaRegistro? Auditoria { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var producto = await _service.ObtenerDetalleAsync(id, _repository);

        if (producto is null)
        {
            return NotFound();
        }

        PublicId = id;
        Producto = producto;
        Auditoria = await _auditoriaService.ObtenerAsync(
            producto.UsuarioCreacionId,
            producto.FechaCreacion,
            producto.UsuarioModificacionId,
            producto.FechaModificacion);
        return Page();
    }
}

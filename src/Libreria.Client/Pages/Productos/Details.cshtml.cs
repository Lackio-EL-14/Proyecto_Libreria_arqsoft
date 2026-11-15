using Libreria.Client.DTOs.Common;
using Libreria.Client.Clients.Productos;
using Libreria.Client.Services.Auditoria;
using Libreria.Client.DTOs.Productos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Productos;

public class DetailsModel : PageModel
{
    private readonly IServicioProductosClient _productos;
    private readonly IAuditoriaService _auditoriaService;

    public DetailsModel(
        IServicioProductosClient productos,
        IAuditoriaService auditoriaService)
    {
        _productos = productos;
        _auditoriaService = auditoriaService;
    }

    public Guid PublicId { get; private set; }

    public ProductoDetalleDto Producto { get; private set; } = null!;
    public AuditoriaRegistroDto? Auditoria { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var producto = await _productos.ObtenerProductoAsync(id);
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

    public async Task<IActionResult> OnPostDarDeBajaAsync(Guid publicId)
    {
        var resultado = await _productos.DarDeBajaProductoAsync(publicId);
        if (!resultado.Exitoso)
        {
            return NotFound();
        }

        TempData["MensajeExito"] = "Producto dado de baja correctamente.";
        return RedirectToPage("./Index");
    }
}

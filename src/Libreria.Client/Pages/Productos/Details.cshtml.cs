using Libreria.Application.Models;
using Libreria.Application.Ports.Primary;
using Libreria.Client.Clients;
using Libreria.Client.DTOs.Ventas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Productos;

public class DetailsModel : PageModel
{
    private readonly IVentasApiClient _ventas;
    private readonly IAuditoriaService _auditoriaService;

    public DetailsModel(
        IVentasApiClient ventas,
        IAuditoriaService auditoriaService)
    {
        _ventas = ventas;
        _auditoriaService = auditoriaService;
    }

    public Guid PublicId { get; private set; }

    public ProductoDetalleDto Producto { get; private set; } = null!;
    public AuditoriaRegistro? Auditoria { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var producto = await _ventas.ObtenerProductoAsync(id);
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
        var resultado = await _ventas.DarDeBajaProductoAsync(publicId);
        if (!resultado.Exitoso)
        {
            return NotFound();
        }

        TempData["MensajeExito"] = "Producto dado de baja correctamente.";
        return RedirectToPage("./Index");
    }
}

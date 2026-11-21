using Libreria.Client.DTOs.Common;
using Libreria.Client.Adapters.Productos;
using Libreria.Client.Services.Auditoria;
using Libreria.Client.DTOs.Productos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Productos;

public class DetailsModel : PageModel
{
    private readonly IProductosApi _productos;
    private readonly IAuditoriaService _auditoriaService;

    public DetailsModel(
        IProductosApi productos,
        IAuditoriaService auditoriaService)
    {
        _productos = productos;
        _auditoriaService = auditoriaService;
    }

    public Guid PublicId { get; private set; }

    public ProductoDetalleDto Producto { get; private set; } = null!;
    public AuditoriaRegistroDto? Auditoria { get; private set; }
    public HistoricoProductoDto? Historico { get; private set; }

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

        var historico =
            await _productos.ObtenerHistoricoAsync(id);

        if (historico is not null)
        {
            var historial =
                await _auditoriaService
                    .ResolverUsuariosHistoricoAsync(
                        historico.Historial);

            Historico = historico with
            {
                Historial = historial
            };
        }

        return Page();
    }

    public async Task<IActionResult> OnPostDarDeBajaAsync(Guid publicId)
    {
        var resultado = await _productos.DarDeBajaProductoAsync(publicId);
        if (!resultado.Exitoso)
        {
            TempData["MensajeError"] = IndexModel.MensajeDeError(resultado, "No se pudo dar de baja el producto.");
            return RedirectToPage("./Details", new { id = publicId });
        }

        TempData["MensajeExito"] = "Producto dado de baja correctamente.";
        return RedirectToPage("./Index");
    }
}

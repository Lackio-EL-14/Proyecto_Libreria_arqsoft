using Libreria.Domain;
using Libreria.Application.Models;
using Libreria.Application.Ports.Primary;
using Libreria.Client.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Ventas;

public class IndexModel : PageModel
{
    private readonly IVentaFacade _ventaFacade;

    public IndexModel(IVentaFacade ventaFacade)
    {
        _ventaFacade = ventaFacade;
    }

    [BindProperty(SupportsGet = true)]
    public FiltroVentasInput Filtro { get; set; } = new();

    public IReadOnlyList<VentaListItemModel> Ventas { get; private set; } = new List<VentaListItemModel>();

    public bool EsAdministrador => User.IsInRole(RolesSistema.Administrador);

    [TempData]
    public string? MensajeExito { get; set; }

    [TempData]
    public string? MensajeError { get; set; }

    public async Task OnGetAsync()
    {
        if (Filtro.FechaDesde.HasValue && Filtro.FechaHasta.HasValue && Filtro.FechaDesde.Value > Filtro.FechaHasta.Value)
        {
            MensajeError = "La 'Fecha desde' debe ser anterior o igual a la 'Fecha hasta'.";
            Ventas = new List<VentaListItemModel>();
            return;
        }

        Ventas = await _ventaFacade.ObtenerListadoAsync(Filtro);
    }

    public async Task<IActionResult> OnGetDetalleAsync(Guid publicId)
    {
        if (publicId == Guid.Empty)
        {
            return BadRequest(new { mensaje = "Identificador de venta inválido." });
        }

        var detalle = await _ventaFacade.ObtenerDetalleCompletoAsync(publicId, EsAdministrador);

        if (detalle is null)
        {
            return NotFound(new { mensaje = "La venta solicitada no fue encontrada." });
        }

        return new JsonResult(detalle);
    }

    public async Task<IActionResult> OnPostAnularAsync(Guid publicId, string motivoAnulacion)
    {
        if (!EsAdministrador)
        {
            return Forbid();
        }

        if (publicId == Guid.Empty)
        {
            MensajeError = "El identificador de la venta es inválido.";
            return RedirectToPage(ObtenerValoresRutaFiltro());
        }

        try
        {
            await _ventaFacade.AnularVentaAsync(publicId, motivoAnulacion);
            MensajeExito = "La venta fue anulada exitosamente y el stock ha sido restaurado.";
        }
        catch (ArgumentException ex)
        {
            MensajeError = ex.Message;
        }
        catch (InvalidOperationException ex)
        {
            MensajeError = ex.Message;
        }
        catch (KeyNotFoundException ex)
        {
            MensajeError = ex.Message;
        }
        catch (Exception)
        {
            MensajeError = "Ocurrió un error inesperado al intentar anular la venta.";
        }

        return RedirectToPage(ObtenerValoresRutaFiltro());
    }

    private object ObtenerValoresRutaFiltro()
    {
        return new
        {
            Estado = Filtro.Estado,
            FechaDesde = Filtro.FechaDesde?.ToString("yyyy-MM-dd"),
            FechaHasta = Filtro.FechaHasta?.ToString("yyyy-MM-dd")
        };
    }
}

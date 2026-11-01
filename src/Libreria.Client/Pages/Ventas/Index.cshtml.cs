using Libreria.Client.Clients;
using Libreria.Client.DTOs.Ventas;
using Libreria.Client.Seguridad;
using Libreria.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Ventas;

public class IndexModel : PageModel
{
    private readonly IVentasApiClient _ventasApiClient;

    public IndexModel(IVentasApiClient ventasApiClient)
    {
        _ventasApiClient = ventasApiClient;
    }

    [BindProperty(SupportsGet = true)]
    public FiltroVentasModel Filtro { get; set; } = new();

    public IReadOnlyList<VentaListItemDto> Ventas { get; private set; } = new List<VentaListItemDto>();

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
            Ventas = new List<VentaListItemDto>();
            return;
        }

        Ventas = await _ventasApiClient.ObtenerVentasAsync(
            Filtro.Estado,
            Filtro.FechaDesde,
            Filtro.FechaHasta);
    }

    public async Task<IActionResult> OnGetDetalleAsync(Guid publicId)
    {
        if (publicId == Guid.Empty)
        {
            return BadRequest(new { mensaje = "Identificador de venta inválido." });
        }

        var detalle = await _ventasApiClient.ObtenerDetalleVentaAsync(publicId, EsAdministrador);

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

        if (string.IsNullOrWhiteSpace(motivoAnulacion) || motivoAnulacion.Trim().Length < 10 || motivoAnulacion.Trim().Length > 250)
        {
            MensajeError = "El motivo de la anulación debe tener entre 10 y 250 caracteres.";
            return RedirectToPage(ObtenerValoresRutaFiltro());
        }

        var resultado = await _ventasApiClient.AnularVentaAsync(publicId, motivoAnulacion.Trim());

        if (resultado.Exitoso)
        {
            MensajeExito = "La venta fue anulada exitosamente y el stock ha sido restaurado.";
        }
        else if (resultado.NoEncontrado)
        {
            MensajeError = "La venta especificada no existe.";
        }
        else
        {
            var primerError = resultado.Errores.Values.FirstOrDefault();
            MensajeError = !string.IsNullOrWhiteSpace(primerError)
                ? primerError
                : "No se pudo anular la venta. Intente nuevamente.";
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

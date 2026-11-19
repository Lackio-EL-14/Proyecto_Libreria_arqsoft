using Libreria.Client.Adapters.Proveedores;
using Libreria.Client.DTOs.Proveedores;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Proveedores;

public sealed class IndexModel : PageModel
{
    private readonly IProveedoresApi _proveedores;

    public IndexModel(IProveedoresApi proveedores)
    {
        _proveedores = proveedores;
    }

    [BindProperty(SupportsGet = true)]
    public string? Busqueda { get; set; }

    [BindProperty(SupportsGet = true)]
    public EstadoFiltroProveedores Estado { get; set; } = EstadoFiltroProveedores.Activos;

    [BindProperty(SupportsGet = true)]
    public OrdenProveedores Orden { get; set; } = OrdenProveedores.RazonSocial;

    [BindProperty(SupportsGet = true)]
    public bool Descendente { get; set; }

    public IReadOnlyList<ProveedorDetalleDto> Proveedores { get; private set; } = [];

    public bool HayFiltros =>
        !string.IsNullOrWhiteSpace(Busqueda)
        || Estado != EstadoFiltroProveedores.Activos;

    public async Task OnGetAsync()
    {
        Busqueda = Busqueda?.Trim();
        Proveedores = await _proveedores.ObtenerProveedoresAsync(
            new FiltroProveedores(Busqueda, Estado, Orden, Descendente));
    }

    public Task<IActionResult> OnPostDarDeBajaAsync(Guid publicId) =>
        CambiarEstadoAsync(publicId, false, "Proveedor dado de baja correctamente.");

    public Task<IActionResult> OnPostReactivarAsync(Guid publicId) =>
        CambiarEstadoAsync(publicId, true, "Proveedor reactivado correctamente.");

    public Dictionary<string, string?> RutaOrden(OrdenProveedores columna) => new()
    {
        ["busqueda"] = Busqueda,
        ["estado"] = Estado.ToString(),
        ["orden"] = columna.ToString(),
        ["descendente"] = (columna == Orden && !Descendente).ToString().ToLowerInvariant()
    };

    public string AriaSort(OrdenProveedores columna) =>
        columna != Orden ? "none" : Descendente ? "descending" : "ascending";

    public string IndicadorOrden(OrdenProveedores columna) =>
        columna != Orden ? string.Empty : Descendente ? "▼" : "▲";

    private async Task<IActionResult> CambiarEstadoAsync(
        Guid publicId,
        bool activo,
        string mensajeExito)
    {
        var resultado = await _proveedores.CambiarEstadoProveedorAsync(publicId, activo);
        if (resultado.Exitoso)
        {
            TempData["MensajeExito"] = mensajeExito;
        }
        else
        {
            TempData["MensajeError"] = resultado.Errores.Values.FirstOrDefault()
                ?? "No se pudo cambiar el estado del proveedor.";
        }

        return RedirectToPage("./Index");
    }
}

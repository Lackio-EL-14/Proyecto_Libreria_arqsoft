using Libreria.Client.Adapters.Clientes;
using Libreria.Client.DTOs.Clientes;
using Libreria.Client.DTOs.Usuarios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Clientes;

/// <summary>
/// Listado de clientes con búsqueda, filtro de estado y orden por columna (lo resuelve el MS).
/// Baja y reactivación solo para Administrador.
/// </summary>
public class IndexModel : PageModel
{
    private readonly IClientesApi _clientes;

    public IndexModel(IClientesApi clientes)
    {
        _clientes = clientes;
    }

    [BindProperty(SupportsGet = true)]
    public string? Busqueda { get; set; }

    [BindProperty(SupportsGet = true)]
    public EstadoFiltroClientes Estado { get; set; } = EstadoFiltroClientes.Activos;

    [BindProperty(SupportsGet = true)]
    public OrdenClientes Orden { get; set; } = OrdenClientes.RazonSocial;

    [BindProperty(SupportsGet = true)]
    public bool Descendente { get; set; }

    public IReadOnlyList<ClienteDetalleDto> Clientes { get; private set; } = [];

    public bool PuedeCambiarEstado => User.IsInRole(RolesSistema.Administrador);

    public bool HayFiltros => !string.IsNullOrWhiteSpace(Busqueda) || Estado != EstadoFiltroClientes.Activos;

    public async Task OnGetAsync()
    {
        Busqueda = Busqueda?.Trim();
        Clientes = await _clientes.ObtenerClientesAsync(
            new FiltroClientes(Busqueda, Estado, Orden, Descendente));
    }

    public Task<IActionResult> OnPostDarDeBajaAsync(Guid publicId) =>
        CambiarEstadoAsync(publicId, activo: false, "Cliente dado de baja correctamente.");

    public Task<IActionResult> OnPostReactivarAsync(Guid publicId) =>
        CambiarEstadoAsync(publicId, activo: true, "Cliente reactivado correctamente.");

    /// <summary>Ruta de un encabezado: misma columna invierte el sentido; otra columna empieza ascendente.</summary>
    public Dictionary<string, string?> RutaOrden(OrdenClientes columna) => new()
    {
        ["busqueda"] = Busqueda,
        ["estado"] = Estado.ToString(),
        ["orden"] = columna.ToString(),
        ["descendente"] = (columna == Orden && !Descendente).ToString().ToLowerInvariant()
    };

    public string AriaSort(OrdenClientes columna) =>
        columna != Orden ? "none" : Descendente ? "descending" : "ascending";

    public string IndicadorOrden(OrdenClientes columna) =>
        columna != Orden ? string.Empty : Descendente ? "▼" : "▲";

    private async Task<IActionResult> CambiarEstadoAsync(Guid publicId, bool activo, string mensajeExito)
    {
        if (!PuedeCambiarEstado)
        {
            return Forbid();
        }

        var resultado = await _clientes.CambiarEstadoClienteAsync(publicId, activo);
        if (resultado.Exitoso)
        {
            TempData["MensajeExito"] = mensajeExito;
        }
        else
        {
            TempData["MensajeError"] = resultado.Errores.Values.FirstOrDefault()
                ?? "No se pudo cambiar el estado del cliente.";
        }

        // Vuelve al listado por defecto (activos): el cliente dado de baja desaparece
        // y el reactivado aparece, así el resultado se ve de inmediato.
        return RedirectToPage("./Index");
    }
}

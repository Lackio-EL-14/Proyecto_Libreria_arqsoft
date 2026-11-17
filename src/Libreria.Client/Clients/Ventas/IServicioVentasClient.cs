using Libreria.Client.Clients.Common;
using Libreria.Client.DTOs.Common;
using Libreria.Client.DTOs.Ventas;

namespace Libreria.Client.Clients.Ventas;

/// <summary>MS Ventas detrás del gateway (ruta <c>api/ventas</c>).</summary>
public interface IServicioVentasClient : IApiClient
{
    Task<IReadOnlyList<VentaListItemDto>> ObtenerVentasAsync(
        string? estado,
        DateOnly? fechaDesde,
        DateOnly? fechaHasta,
        OrdenVentasDto orden,
        bool descendente,
        CancellationToken ct = default);
    Task<VentaDetalleDto?> ObtenerDetalleVentaAsync(Guid publicId, CancellationToken ct = default);
    Task<ResultadoApi> CorregirVentaAsync(Guid publicId, CorregirVentaDto input, CancellationToken ct = default);
    Task<ResultadoApi> AnularVentaAsync(Guid publicId, string motivo, CancellationToken ct = default);
    Task<ComprobanteVentaDto?> ObtenerComprobanteAsync(Guid publicId, CancellationToken ct = default);
}

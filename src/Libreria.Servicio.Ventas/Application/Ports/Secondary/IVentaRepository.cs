using System.Data;
using Libreria.Servicio.Ventas.Domain;
using Libreria.Servicio.Ventas.Application.Models;

namespace Libreria.Servicio.Ventas.Application.Ports.Secondary;

public interface IVentaRepository
{
    Task<int> InsertarAsync(
        Venta venta,
        IDbConnection connection,
        IDbTransaction transaction);

    Task InsertarDetalleAsync(
        DetalleVenta detalle,
        IDbConnection connection,
        IDbTransaction transaction);

    Task<bool> ActualizarEstadoAsync(
        int ventaId,
        string estado,
        int? usuarioAnulacionId,
        string? motivoAnulacion,
        IDbConnection connection,
        IDbTransaction transaction);

    Task<IReadOnlyList<Venta>> ObtenerActivasAsync();

    Task<IReadOnlyList<Venta>> ObtenerTodasAsync();

    Task<Venta?> ObtenerPorPublicIdAsync(Guid publicId);

    Task<Venta?> ObtenerPorPublicIdAsync(
        Guid publicId,
        IDbConnection connection,
        IDbTransaction transaction);

    Task<IReadOnlyList<DetalleVenta>> ObtenerDetallePorVentaIdAsync(
        int ventaId);

    Task<IReadOnlyList<DetalleVenta>> ObtenerDetallePorVentaIdAsync(
        int ventaId,
        IDbConnection connection,
        IDbTransaction transaction);

    Task<IReadOnlyList<VentaListItemModel>> ObtenerListadoAsync(
        FiltroVentasInput filtro);

    Task<VentaDetalleCompletoModel?> ObtenerDetalleCompletoAsync(
        Guid publicId,
        bool esAdministrador);

    Task<ComprobanteVenta?> ObtenerComprobanteAsync(Guid publicId);
}

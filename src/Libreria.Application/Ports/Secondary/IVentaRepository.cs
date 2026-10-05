using Libreria.Application.Domain;
using System.Data;

namespace Libreria.Application.Ports.Secondary;

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
        IDbConnection connection,
        IDbTransaction transaction);

    Task<IReadOnlyList<Venta>> ObtenerActivasAsync();

    Task<Venta?> ObtenerPorPublicIdAsync(Guid publicId);

    Task<IReadOnlyList<DetalleVenta>> ObtenerDetallePorVentaIdAsync(
        int ventaId);
}
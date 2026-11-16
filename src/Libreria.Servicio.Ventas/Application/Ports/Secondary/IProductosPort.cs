using Libreria.Servicio.Ventas.Application.Comun;
using Libreria.Servicio.Ventas.Application.Models;

namespace Libreria.Servicio.Ventas.Application.Ports.Secondary;

public interface IProductosPort
{
    Task<Result<ProductoVentaSnapshot>> ObtenerPorPublicIdAsync(
        Guid publicId,
        CancellationToken cancellationToken = default);

    Task<Result> DescontarStockAsync(
        Guid productoPublicId,
        int cantidad,
        Guid ventaPublicId,
        CancellationToken cancellationToken = default);

    Task<Result> RestaurarStockAsync(
        Guid productoPublicId,
        int cantidad,
        Guid ventaPublicId,
        CancellationToken cancellationToken = default);
}

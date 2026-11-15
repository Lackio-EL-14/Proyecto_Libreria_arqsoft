using Libreria.Servicio.Ventas.Application.Models;

namespace Libreria.Servicio.Ventas.Application.Ports.Secondary;

public interface IProductosPort
{
    Task<ProductoVentaSnapshot?> ObtenerPorPublicIdAsync(Guid publicId);

    Task<bool> DescontarStockAsync(
        Guid productoPublicId,
        int cantidad,
        Guid ventaPublicId);

    Task<bool> RestaurarStockAsync(
        Guid productoPublicId,
        int cantidad,
        Guid ventaPublicId);
}

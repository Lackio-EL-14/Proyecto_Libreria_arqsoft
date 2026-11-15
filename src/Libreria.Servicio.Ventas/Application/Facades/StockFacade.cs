using Libreria.Servicio.Ventas.Application.Ports.Primary;
using Libreria.Servicio.Ventas.Application.Ports.Secondary;

namespace Libreria.Servicio.Ventas.Application.Facades;

public class StockFacade : IStockFacade
{
    private readonly IProductosPort _productos;

    public StockFacade(IProductosPort productos)
    {
        _productos = productos;
    }

    public async Task DescontarStockAsync(
        Guid productoPublicId,
        int cantidad,
        Guid ventaPublicId)
    {
        if (cantidad <= 0)
        {
            throw new ArgumentException(
                "La cantidad a descontar debe ser mayor a cero.",
                nameof(cantidad));
        }

        if (!await _productos.DescontarStockAsync(
                productoPublicId,
                cantidad,
                ventaPublicId))
        {
            throw new InvalidOperationException(
                "No existe stock suficiente para realizar la venta.");
        }
    }

    public async Task RestaurarStockAsync(
        Guid productoPublicId,
        int cantidad,
        Guid ventaPublicId)
    {
        if (cantidad <= 0)
        {
            throw new ArgumentException(
                "La cantidad a restaurar debe ser mayor a cero.",
                nameof(cantidad));
        }

        if (!await _productos.RestaurarStockAsync(
                productoPublicId,
                cantidad,
                ventaPublicId))
        {
            throw new InvalidOperationException(
                "No se pudo restaurar el stock del producto.");
        }
    }
}

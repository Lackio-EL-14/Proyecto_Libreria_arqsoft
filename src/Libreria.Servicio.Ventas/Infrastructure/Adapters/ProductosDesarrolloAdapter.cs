using Libreria.Servicio.Ventas.Application.Models;
using Libreria.Servicio.Ventas.Application.Ports.Secondary;

namespace Libreria.Servicio.Ventas.Infrastructure.Adapters;

public sealed class ProductosDesarrolloAdapter : IProductosPort
{
    private readonly object _bloqueo = new();
    private readonly Dictionary<Guid, ProductoVentaSnapshot> _productos = CrearProductos();

    public Task<ProductoVentaSnapshot?> ObtenerPorPublicIdAsync(Guid publicId)
    {
        lock (_bloqueo)
        {
            _productos.TryGetValue(publicId, out var producto);
            return Task.FromResult(producto);
        }
    }

    public Task<bool> DescontarStockAsync(
        Guid productoPublicId,
        int cantidad,
        Guid ventaPublicId)
    {
        lock (_bloqueo)
        {
            if (!_productos.TryGetValue(productoPublicId, out var producto)
                || cantidad <= 0
                || producto.Stock < cantidad)
            {
                return Task.FromResult(false);
            }

            _productos[productoPublicId] = producto with
            {
                Stock = producto.Stock - cantidad
            };

            return Task.FromResult(true);
        }
    }

    public Task<bool> RestaurarStockAsync(
        Guid productoPublicId,
        int cantidad,
        Guid ventaPublicId)
    {
        lock (_bloqueo)
        {
            if (!_productos.TryGetValue(productoPublicId, out var producto)
                || cantidad <= 0)
            {
                return Task.FromResult(false);
            }

            _productos[productoPublicId] = producto with
            {
                Stock = producto.Stock + cantidad
            };

            return Task.FromResult(true);
        }
    }

    private static Dictionary<Guid, ProductoVentaSnapshot> CrearProductos()
    {
        var productos = new[]
        {
            new ProductoVentaSnapshot(
                Guid.Parse("d0000000-0000-0000-0000-000000000001"),
                "Cuaderno Espiral 100 hojas", 150, 15m, 10m),
            new ProductoVentaSnapshot(
                Guid.Parse("d0000000-0000-0000-0000-000000000002"),
                "Caja Bolígrafos Azules x12", 80, 25m, 18m),
            new ProductoVentaSnapshot(
                Guid.Parse("d0000000-0000-0000-0000-000000000003"),
                "Libro Matemáticas 5to Sec", 40, 120m, 90m),
            new ProductoVentaSnapshot(
                Guid.Parse("d0000000-0000-0000-0000-000000000004"),
                "Historia Universal 2026", 25, 180m, 130m),
            new ProductoVentaSnapshot(
                Guid.Parse("d0000000-0000-0000-0000-000000000005"),
                "Calculadora Científica", 30, 85m, 60m)
        };

        return productos.ToDictionary(producto => producto.PublicId);
    }
}

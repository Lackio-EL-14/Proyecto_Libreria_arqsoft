using Libreria.Servicio.Ventas.Application.Comun;
using Libreria.Servicio.Ventas.Application.Facades;
using Libreria.Servicio.Ventas.Application.Models;
using Libreria.Servicio.Ventas.Application.Ports.Secondary;

namespace Libreria.Servicio.Ventas.Tests;

public class StockFacadeTests
{
    [Fact]
    public async Task DescontarAsync_SiFallaUnaLinea_RestauraLasAnteriores()
    {
        var primero = Guid.NewGuid();
        var segundo = Guid.NewGuid();
        var productos = new ProductosPortFake(new Dictionary<Guid, int>
        {
            [primero] = 10,
            [segundo] = 1
        });
        var facade = new StockFacade(productos);

        var resultado = await facade.DescontarAsync(
        [
            new StockOperacionItem(primero, 3),
            new StockOperacionItem(segundo, 2)
        ],
        Guid.NewGuid());

        Assert.False(resultado.EsExito);
        Assert.Equal(TipoError.Conflicto, resultado.Tipo);
        Assert.Equal(10, productos.Stock[primero]);
        Assert.Equal(1, productos.Stock[segundo]);
        Assert.Equal([primero], productos.Restauraciones);
    }

    private sealed class ProductosPortFake : IProductosPort
    {
        public ProductosPortFake(Dictionary<Guid, int> stock)
        {
            Stock = stock;
        }

        public Dictionary<Guid, int> Stock { get; }
        public List<Guid> Restauraciones { get; } = [];

        public Task<Result<ProductoVentaSnapshot>> ObtenerPorPublicIdAsync(
            Guid publicId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Result> DescontarStockAsync(
            Guid productoPublicId,
            int cantidad,
            Guid ventaPublicId,
            CancellationToken cancellationToken = default)
        {
            if (!Stock.TryGetValue(productoPublicId, out var disponible)
                || disponible < cantidad)
            {
                return Task.FromResult(Result.Conflicto(
                    "Stock",
                    "No existe stock suficiente."));
            }

            Stock[productoPublicId] = disponible - cantidad;
            return Task.FromResult(Result.Exito());
        }

        public Task<Result> RestaurarStockAsync(
            Guid productoPublicId,
            int cantidad,
            Guid ventaPublicId,
            CancellationToken cancellationToken = default)
        {
            Restauraciones.Add(productoPublicId);
            Stock[productoPublicId] += cantidad;
            return Task.FromResult(Result.Exito());
        }
    }
}

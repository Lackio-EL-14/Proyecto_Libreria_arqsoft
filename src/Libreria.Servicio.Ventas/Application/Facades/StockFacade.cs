using Libreria.Domain;
using Libreria.Servicio.Ventas.Application.Models;
using Libreria.Servicio.Ventas.Application.Ports.Secondary;
using Libreria.Servicio.Ventas.Application.Factories;
using Libreria.Servicio.Ventas.Application.Ports.Primary;
using System.Data;

namespace Libreria.Servicio.Ventas.Application.Facades
{
    public class StockFacade : IStockFacade
    {
        private readonly IProductoRepository _productoRepository;

        public StockFacade(IProductoRepository productoRepository)
        {
            _productoRepository = productoRepository;
        }

        public async Task DescontarStockAsync(
            int productoId,
            int cantidad,
            IDbConnection connection,
            IDbTransaction transaction)
        {
            if (cantidad <= 0)
            {
                throw new ArgumentException(
                    "La cantidad a descontar debe ser mayor a cero.",
                    nameof(cantidad));
            }

            var actualizado = await _productoRepository.ActualizarStockAsync(
                productoId,
                -cantidad,
                connection,
                transaction);

            if (!actualizado)
            {
                throw new InvalidOperationException(
                    "No existe stock suficiente para realizar la venta.");
            }
        }
        public async Task RestaurarStockAsync(
            int productoId,
            int cantidad,
            IDbConnection connection,
            IDbTransaction transaction)
        {
            if (cantidad <= 0)
            {
                throw new ArgumentException(
                    "La cantidad a restaurar debe ser mayor a cero.",
                    nameof(cantidad));
            }

            var actualizado = await _productoRepository.RestaurarStockAsync(
                productoId,
                cantidad,
                connection,
                transaction);

            if (!actualizado)
            {
                throw new InvalidOperationException(
                    "No se pudo restaurar el stock del producto.");
            }
        }
    }
}

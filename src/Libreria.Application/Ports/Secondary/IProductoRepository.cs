using Libreria.Application.Domain;
using System.Data;

namespace Libreria.Application.Ports.Secondary;

public interface IProductoRepository : ICrudRepository<Producto>
{
    Task<bool> ActualizarStockAsync(
        int productoId,
        int cantidad,
        IDbConnection connection,
        IDbTransaction transaction);

    Task<decimal?> ObtenerCostoAdquisicionActualAsync(int productoId);
}
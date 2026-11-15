
using System.Data;

namespace Libreria.Servicio.Ventas.Application.Ports.Secondary;

public interface IMovimientoStockRepository
{
    Task RegistrarAsync(
        int ventaId,
        int productoId,
        int cantidad,
        string tipoMovimiento,
        IDbConnection connection,
        IDbTransaction transaction);
}

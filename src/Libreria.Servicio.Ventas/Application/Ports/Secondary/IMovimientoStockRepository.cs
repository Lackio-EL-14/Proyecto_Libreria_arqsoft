
using System.Data;

namespace Libreria.Servicio.Ventas.Application.Ports.Secondary;

public interface IMovimientoStockRepository
{
    Task RegistrarAsync(
        int ventaId,
        Guid productoPublicId,
        int cantidad,
        string tipoMovimiento,
        IDbConnection connection,
        IDbTransaction transaction);
}

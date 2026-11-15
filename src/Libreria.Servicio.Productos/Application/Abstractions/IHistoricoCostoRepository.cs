using Libreria.Servicio.Productos.Domain.Entities;

namespace Libreria.Servicio.Productos.Application.Abstractions;

public interface IHistoricoCostoRepository
{
    Task<Producto?> ObtenerProductoAsync(Guid publicId);

    Task<IReadOnlyList<HistoricoCosto>> ObtenerHistoricoAsync(
        Guid productoPublicId);
}
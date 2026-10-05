using Libreria.Application.Domain;
using Libreria.Application.Models;
using Libreria.Application.Ports.Secondary;
using Libreria.Application.Factories;

namespace Libreria.Application.Ports.Secondary;

public interface IHistoricoCostoRepository
{
    ProductoHistoricoResumen? ObtenerProducto(Guid publicId);
    IReadOnlyList<HistoricoCostoItem> ObtenerHistorico(int productoId);
}

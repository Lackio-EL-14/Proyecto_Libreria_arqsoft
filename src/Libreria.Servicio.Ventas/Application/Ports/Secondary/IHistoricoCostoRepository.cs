using Libreria.Servicio.Ventas.Domain;
using Libreria.Servicio.Ventas.Application.Models;
using Libreria.Servicio.Ventas.Application.Ports.Secondary;
using Libreria.Servicio.Ventas.Application.Factories;

namespace Libreria.Servicio.Ventas.Application.Ports.Secondary;

public interface IHistoricoCostoRepository
{
    ProductoHistoricoResumen? ObtenerProducto(Guid publicId);
    IReadOnlyList<HistoricoCostoItem> ObtenerHistorico(int productoId);
}

using Libreria.Servicio.Ventas.Application.Comun;
using Libreria.Servicio.Ventas.Application.Models;

namespace Libreria.Servicio.Ventas.Application.Ports.Primary;

public interface IStockFacade
{
    Task<Result> DescontarAsync(
        IReadOnlyList<StockOperacionItem> lineas,
        Guid ventaPublicId,
        CancellationToken cancellationToken = default);

    Task<Result> RestaurarAsync(
        IReadOnlyList<StockOperacionItem> lineas,
        Guid ventaPublicId,
        CancellationToken cancellationToken = default);
}

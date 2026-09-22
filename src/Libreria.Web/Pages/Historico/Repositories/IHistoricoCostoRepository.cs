using Libreria.Web.Pages.Historico.Models;

namespace Libreria.Web.Pages.Historico.Repositories;

public interface IHistoricoCostoRepository
{
    ProductoHistoricoResumen? ObtenerProducto(Guid publicId);
    IReadOnlyList<HistoricoCostoItem> ObtenerHistorico(int productoId);
}

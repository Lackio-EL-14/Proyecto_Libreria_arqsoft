using Libreria.Web.Pages.Historico.Models;

namespace Libreria.Web.Pages.Historico.Repositories;

public interface IHistoricoCostoRepository
{
    ProductoHistoricoResumen? ObtenerProducto(int productoId);
    IReadOnlyList<HistoricoCostoItem> ObtenerHistorico(int productoId);
}

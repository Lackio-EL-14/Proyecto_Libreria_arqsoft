using Libreria.Web.Pages.Productos.Models;

namespace Libreria.Web.Pages.Productos.Repositories;

public interface IProductoRepository
{
    IReadOnlyList<ProductoListItem> ObtenerProductos(
        string? busqueda,
        int? categoriaId,
        int? marcaId);
    ProductoDetalle? ObtenerActivoPorId(int productoId);
    int CrearConHistorico(ProductoInput input);
    bool ActualizarConHistorico(int productoId, ProductoInput input, string motivo);
    bool DarDeBaja(int productoId);
}

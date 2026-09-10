using Libreria.Web.Pages.Productos.Models;

namespace Libreria.Web.Pages.Productos.Services;

public interface IProductoService
{
    ProductoListado ObtenerListado(string? busqueda, int? categoriaId, int? marcaId);
    ProductoFormulario ObtenerFormulario();
    ResultadoOperacion Registrar(ProductoInput input);
    ProductoEdicion? ObtenerEdicion(int productoId);
    ResultadoOperacion Actualizar(int productoId, ProductoInput input);
    ProductoDetalle? ObtenerDetalle(int productoId);
    ProductoDetalle? ObtenerParaBaja(int productoId);
    bool DarDeBaja(int productoId);
}

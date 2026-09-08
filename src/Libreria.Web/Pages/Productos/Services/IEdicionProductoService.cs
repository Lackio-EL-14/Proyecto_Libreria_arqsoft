using Libreria.Web.Pages.Productos.Models;

namespace Libreria.Web.Pages.Productos.Services;

public interface IEdicionProductoService
{
    ProductoEdicion? ObtenerEdicion(int productoId);
    ResultadoOperacion Actualizar(int productoId, ProductoInput input);
}

using Libreria.Web.Pages.Productos.Models;

namespace Libreria.Web.Pages.Productos.Services;

public interface IBajaProductoService
{
    ProductoDetalle? ObtenerParaBaja(int productoId);
    bool DarDeBaja(int productoId);
}

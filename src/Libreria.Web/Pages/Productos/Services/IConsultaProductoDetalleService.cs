using Libreria.Web.Pages.Productos.Models;

namespace Libreria.Web.Pages.Productos.Services;

public interface IConsultaProductoDetalleService
{
    ProductoDetalle? ObtenerDetalle(int productoId);
}

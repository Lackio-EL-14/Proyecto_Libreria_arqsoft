using Libreria.Web.Pages.Productos.Models;

namespace Libreria.Web.Pages.Productos.Services;

public interface IConsultaProductosService
{
    ProductoListado ObtenerListado(string? busqueda, int? categoriaId, int? marcaId);
}

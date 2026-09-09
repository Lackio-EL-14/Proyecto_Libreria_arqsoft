using Libreria.Web.Pages.Productos.Models;

namespace Libreria.Web.Pages.Productos.Services;

public interface IRegistroProductoService
{
    ProductoFormulario ObtenerFormulario();
    ResultadoOperacion Registrar(ProductoInput input);
}

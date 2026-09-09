namespace Libreria.Web.Pages.Productos.Services;

public interface IComparadorCostoProducto
{
    bool Cambio(decimal costoActual, decimal nuevoCosto);
}

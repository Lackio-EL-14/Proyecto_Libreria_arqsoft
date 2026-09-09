namespace Libreria.Web.Pages.Productos.Services;

public class CostoProductoService : IComparadorCostoProducto
{
    public bool Cambio(decimal costoActual, decimal nuevoCosto)
    {
        return costoActual != nuevoCosto;
    }
}

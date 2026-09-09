namespace Libreria.Web.Pages.Productos.Services;

public class CostoProductoService
{
    public bool Cambio(decimal costoActual, decimal nuevoCosto)
    {
        return costoActual != nuevoCosto;
    }
}

using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Web.Pages.Productos;

public class IndexModel : PageModel
{
    public void OnGet()
    {
        // TODO (US-09 a US-12): seguir el patrón de
        // Pages/Categorias/Index.cshtml.cs.
        // IMPORTANTE: el Alta (US-09) y la Edición (US-11) de Producto
        // deben invocar el registro de histórico de costo cuando cambie
        // el costo de adquisición. Ver db/schema.sql, tabla
        // HistoricoCostoProducto, y coordinar la función/servicio con
        // el resto del equipo ANTES de programar (evita bloqueos).
    }
}

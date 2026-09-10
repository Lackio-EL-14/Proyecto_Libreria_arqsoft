using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Web.Pages.Historico;

public class IndexModel : PageModel
{
    public void OnGet()
    {
        // TODO (US-14): consulta de solo lectura sobre
        // HistoricoCostoProducto. Se puede maquetar con datos de
        // prueba mientras Productos (US-09/US-13) genera datos reales.
    }
}

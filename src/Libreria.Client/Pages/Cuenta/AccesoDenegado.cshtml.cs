using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Cuenta;

public class AccesoDenegadoModel : PageModel
{
    public void OnGet()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
    }
}

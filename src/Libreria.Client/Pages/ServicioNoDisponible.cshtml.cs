using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages;

[AllowAnonymous]
public sealed class ServicioNoDisponibleModel : PageModel
{
    public string Servicio { get; private set; } = "solicitado";

    public void OnGet(string? servicio)
    {
        if (!string.IsNullOrWhiteSpace(servicio))
        {
            Servicio = servicio;
        }
    }
}

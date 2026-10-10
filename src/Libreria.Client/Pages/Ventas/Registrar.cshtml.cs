using Libreria.Application.Ports.Primary;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Ventas;

public class RegistrarModel : PageModel
{
    private readonly IConsultaVentaService _consulta;

    public RegistrarModel(IConsultaVentaService consulta)
    {
        _consulta = consulta;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnGetBuscarProductosAsync(string? nombre)
    {
        return new JsonResult(await _consulta.BuscarProductosAsync(nombre));
    }

    public async Task<IActionResult> OnGetBuscarClientesAsync(string? ciNit)
    {
        return new JsonResult(await _consulta.BuscarClientesAsync(ciNit));
    }
}

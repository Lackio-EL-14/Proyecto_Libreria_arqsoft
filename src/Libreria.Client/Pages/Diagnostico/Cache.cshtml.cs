using Libreria.Application.Models;
using Libreria.Application.Services;
using Libreria.Client.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Diagnostico;

[Authorize(Policy = PoliticasAcceso.SoloAdministrador)]
public class CacheModel : PageModel
{
    private readonly CacheDiagnosticoService _diagnosticoService;

    public CacheModel(CacheDiagnosticoService diagnosticoService)
    {
        _diagnosticoService = diagnosticoService;
    }

    public CacheDiagnostico? UltimoResultado { get; private set; }

    public async Task OnGetAsync()
    {
        UltimoResultado = await _diagnosticoService.ObtenerUltimoAsync();
    }
}

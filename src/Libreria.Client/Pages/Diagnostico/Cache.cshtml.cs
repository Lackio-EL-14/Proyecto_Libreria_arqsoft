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
    private readonly string _instanceName;

    public CacheModel(
        CacheDiagnosticoService diagnosticoService,
        IConfiguration configuration)
    {
        _diagnosticoService = diagnosticoService;
        _instanceName = configuration["Redis:InstanceName"]
            ?? configuration["REDIS_INSTANCE_NAME"]
            ?? "libreria:";
    }

    public CacheDiagnostico? UltimoResultado { get; private set; }
    public string ClaveProductoFisica =>
        $"{_instanceName}{UltimoResultado?.Clave}";
    public string PatronSesiones => $"{_instanceName}sesiones:*";

    public async Task OnGetAsync()
    {
        UltimoResultado = await _diagnosticoService.ObtenerUltimoAsync();
    }
}

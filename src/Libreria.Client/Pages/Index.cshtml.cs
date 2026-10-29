using Libreria.Client.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages;

public class IndexModel : PageModel
{
    private readonly IAuthorizationService _authorizationService;

    public IndexModel(IAuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;
    }

    public bool EsAdministrador { get; private set; }

    public async Task OnGetAsync()
    {
        EsAdministrador = (await _authorizationService
            .AuthorizeAsync(User, PoliticasAcceso.SoloAdministrador)).Succeeded;
    }
}

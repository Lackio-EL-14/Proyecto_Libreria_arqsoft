using Libreria.Application.Models;
using Libreria.Application.Ports.Primary;
using Libreria.Client.Seguridad;
using Libreria.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Usuarios;

[Authorize(Policy = PoliticasAcceso.SoloAdministrador)]
public class IndexModel : PageModel
{
    private readonly IConsultaUsuarioService _consulta;
    private readonly IAuthorizationService _authorizationService;

    public IndexModel(IConsultaUsuarioService consulta, IAuthorizationService authorizationService)
    {
        _consulta = consulta;
        _authorizationService = authorizationService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Estado { get; set; }

    public IReadOnlyList<string> Estados => EstadosUsuario.Todos;
    public IReadOnlyList<UsuarioListItemModel> Usuarios { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Challenge();
        }

        if (!(await _authorizationService.AuthorizeAsync(User, PoliticasAcceso.SoloAdministrador)).Succeeded)
        {
            return Forbid();
        }

        Estado = Estado?.Trim();

        try
        {
            Usuarios = await _consulta.ObtenerListadoAsync(Estado);
        }
        catch (ArgumentException error)
        {
            ModelState.AddModelError(nameof(Estado), error.Message);
        }

        return Page();
    }
}

using Libreria.Client.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Libreria.Client.Adapters.Usuarios;
using Libreria.Client.DTOs.Usuarios;
using Libreria.Client.Clients.Common;

namespace Libreria.Client.Pages.Usuarios;

[Authorize(Policy = PoliticasAcceso.SoloAdministrador)]
public class IndexModel : PageModel
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IUrlProtector _protector;
    private readonly IUsuariosApi _usuariosClient;

    public IndexModel(
        IAuthorizationService authorizationService,
        IUrlProtector protector,
        IUsuariosApi usuariosClient)
    {
        _authorizationService = authorizationService;
        _protector = protector;
        _usuariosClient = usuariosClient;
    }

    [BindProperty(SupportsGet = true)]
    public string? Estado { get; set; }

    /// <summary>Columna de orden que aplica el MS Usuarios: nombre, usuario, correo, rol o estado.</summary>
    [BindProperty(SupportsGet = true)]
    public string Orden { get; set; } = "nombre";

    [BindProperty(SupportsGet = true)]
    public bool Descendente { get; set; }

    /// <summary>Ruta para ordenar por <paramref name="columna"/>: si ya ordena por ella, invierte la dirección.</summary>
    public Dictionary<string, string> RutaOrden(string columna) => new()
    {
        ["Estado"] = Estado ?? string.Empty,
        ["Orden"] = columna,
        ["Descendente"] = (string.Equals(Orden, columna, StringComparison.OrdinalIgnoreCase) && !Descendente).ToString().ToLowerInvariant()
    };

    public string IndicadorOrden(string columna) =>
        !string.Equals(Orden, columna, StringComparison.OrdinalIgnoreCase) ? string.Empty : Descendente ? " ▼" : " ▲";

    public Dictionary<string, string> RutaActual => new()
    {
        ["Estado"] = Estado ?? string.Empty,
        ["Orden"] = Orden,
        ["Descendente"] = Descendente.ToString().ToLowerInvariant()
    };

    public IReadOnlyList<string> Estados => EstadosUsuario.Todos;
    public IReadOnlyList<UserListItemDto> Usuarios { get; private set; } = [];

    public string ProtegerId(Guid publicId) => _protector.Cifrar(publicId);

    public async Task<IActionResult> OnPostDarDeBajaAsync(
        string? publicId)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Challenge();
        }

        if (!(await _authorizationService.AuthorizeAsync(
                User,
                PoliticasAcceso.SoloAdministrador)).Succeeded)
        {
            return Forbid();
        }

        var identificador =
            string.IsNullOrWhiteSpace(publicId)
                ? null
                : _protector.Descifrar(publicId);

        if (!identificador.HasValue ||
            identificador == Guid.Empty)
        {
            return BadRequest();
        }

        try
        {
            await _usuariosClient.DarDeBajaUsuarioAsync(
                identificador.Value,
                HttpContext.RequestAborted);

            TempData["MensajeExito"] =
                "Usuario dado de baja correctamente.";
        }
        catch (ApiClientException error)
        {
            TempData["MensajeError"] = error.Message;
        }

        return RedirectToPage(
            "./Index",
            new { Estado, Orden, Descendente });
    }

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
            Usuarios = await _usuariosClient.ObtenerUsuariosAsync(
                Estado, Orden, Descendente, HttpContext.RequestAborted);
        }
        catch (ApiClientException error)
        {
            ModelState.AddModelError(string.Empty, error.Message);
        }

        return Page();
    }
}

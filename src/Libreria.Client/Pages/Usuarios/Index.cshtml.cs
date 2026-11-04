using Libreria.Application.Models;
using Libreria.Application.Ports.Primary;
using Libreria.Client.Seguridad;
using Libreria.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Libreria.Client.Clients.Usuarios;
using Libreria.Client.DTOs.Usuarios;
using Libreria.Client.Clients.Common;

namespace Libreria.Client.Pages.Usuarios;

[Authorize(Policy = PoliticasAcceso.SoloAdministrador)]
public class IndexModel : PageModel
{
   private readonly IAuthorizationService _authorizationService;
    private readonly IUrlProtector _protector;
    private readonly IServicioUsuariosClient _usuariosClient;

    public IndexModel(
        IAuthorizationService authorizationService,
        IUrlProtector protector,
        IServicioUsuariosClient usuariosClient)
    {
        _authorizationService = authorizationService;
        _protector = protector;
        _usuariosClient = usuariosClient;
    }

    [BindProperty(SupportsGet = true)]
    public string? Estado { get; set; }

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
            new { Estado });
    }

    public async Task<IActionResult> OnPostReenviarActivacionAsync(
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

        if (!identificador.HasValue || identificador == Guid.Empty)
        {
            return BadRequest();
        }

        try
        {
            await _usuariosClient.ReenviarActivacionAsync(
                identificador.Value,
                HttpContext.RequestAborted);

            TempData["MensajeExito"] =
                "Se reenvió el correo de activación.";
        }
        catch (ApiClientException error)
        {
            TempData["MensajeError"] = error.Message;
        }

        return RedirectToPage(
            "./Index",
            new { Estado });
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
            Usuarios = await _usuariosClient.ObtenerUsuariosAsync(Estado);
        }
        catch (ArgumentException error)
        {
            ModelState.AddModelError(nameof(Estado), error.Message);
        }

        return Page();
    }
}

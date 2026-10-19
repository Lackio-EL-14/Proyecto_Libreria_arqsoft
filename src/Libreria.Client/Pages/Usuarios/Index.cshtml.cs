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
    private readonly IUsuarioSeguridadService _seguridad;
    private readonly IUrlProtector _protector;

    public IndexModel(IConsultaUsuarioService consulta, IAuthorizationService authorizationService,
        IUsuarioSeguridadService seguridad, IUrlProtector protector)
    {
        _consulta = consulta;
        _authorizationService = authorizationService;
        _seguridad = seguridad;
        _protector = protector;
    }

    [BindProperty(SupportsGet = true)]
    public string? Estado { get; set; }

    public IReadOnlyList<string> Estados => EstadosUsuario.Todos;
    public IReadOnlyList<UsuarioListItemModel> Usuarios { get; private set; } = [];

    public string ProtegerId(Guid publicId) => _protector.Cifrar(publicId);

    public async Task<IActionResult> OnPostDarDeBajaAsync(string? publicId)
    {
        if (User.Identity?.IsAuthenticated != true)
            return Challenge();
        if (!(await _authorizationService.AuthorizeAsync(User, PoliticasAcceso.SoloAdministrador)).Succeeded)
            return Forbid();

        var identificador = string.IsNullOrWhiteSpace(publicId) ? null : _protector.Descifrar(publicId);
        if (!identificador.HasValue || identificador == Guid.Empty)
            return BadRequest();

        var resultado = await _seguridad.DarDeBajaAsync(identificador.Value);
        if (resultado.NoEncontrado)
            return NotFound();
        if (resultado.Exitoso)
            TempData["MensajeExito"] = "Usuario dado de baja correctamente.";
        else
            TempData["MensajeError"] = string.Join(" ", resultado.Errores.Values);

        return RedirectToPage("./Index", new { Estado });
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
            Usuarios = await _consulta.ObtenerListadoAsync(Estado);
        }
        catch (ArgumentException error)
        {
            ModelState.AddModelError(nameof(Estado), error.Message);
        }

        return Page();
    }
}

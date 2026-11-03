using Libreria.Client.Clients.Common;
using Libreria.Client.Clients.Usuarios;
using Libreria.Client.DTOs.Usuarios;
using Libreria.Client.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Libreria.Client.Pages.Usuarios;

[Authorize(Policy = PoliticasAcceso.SoloAdministrador)]
public class CreateModel : PageModel
{
    private readonly IServicioUsuariosClient _usuariosClient;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(
        IServicioUsuariosClient usuariosClient,
        ILogger<CreateModel> logger)
    {
        _usuariosClient = usuariosClient;
        _logger = logger;
    }

    [BindProperty]
    public string Nombre { get; set; } = string.Empty;

    [BindProperty]
    public string Apellido { get; set; } = string.Empty;

    [BindProperty]
    public string Correo { get; set; } = string.Empty;

    [BindProperty]
    public string Rol { get; set; } = string.Empty;

    public string NombreUsuario { get; private set; } = string.Empty;

    public string MensajeExito { get; private set; } = string.Empty;

    public IReadOnlyList<SelectListItem> Roles { get; private set; } = [];

    public async Task OnGetAsync()
    {
        await CargarRolesAsync();
    }

    public async Task<IActionResult> OnPostCrearAsync()
    {
        await CargarRolesAsync();

        if (!Roles.Any(rol =>
                string.Equals(
                    rol.Value,
                    Rol,
                    StringComparison.OrdinalIgnoreCase)))
        {
            ModelState.AddModelError(
                nameof(Rol),
                "Debe seleccionar un rol válido.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var result =
                await _usuariosClient.CrearUsuarioAsync(
                    new CreateUserRequestDto(
                        Nombre,
                        Apellido,
                        Correo,
                        Rol));

            if (result is null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "No fue posible crear el usuario.");

                return Page();
            }

            NombreUsuario = result.Username;

            MensajeExito =
                $"Usuario {NombreUsuario} creado correctamente. " +
                "Se generó el enlace de activación para el correo registrado.";

            return Page();
        }
        catch (ApiClientException error)
        {
            ModelState.AddModelError(
                string.Empty,
                error.Message);

            return Page();
        }
        catch (ServicioNoDisponibleException error)
        {
            _logger.LogError(
                error,
                "El servicio de Usuarios no está disponible.");

            ModelState.AddModelError(
                string.Empty,
                "El servicio de Usuarios no está disponible.");

            return Page();
        }
    }

    private async Task CargarRolesAsync()
    {
        var roles =
            await _usuariosClient.ObtenerRolesAsync();

        Roles = roles
            .Select(rol => new SelectListItem
            {
                Value = rol,
                Text = rol
            })
            .ToList();
    }
}
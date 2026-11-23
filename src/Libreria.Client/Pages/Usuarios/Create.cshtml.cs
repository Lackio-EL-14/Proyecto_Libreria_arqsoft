using System.ComponentModel.DataAnnotations;
using Libreria.Client.Clients.Common;
using Libreria.Client.Adapters.Usuarios;
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
    private readonly IUsuariosApi _usuariosClient;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(
        IUsuariosApi usuariosClient,
        ILogger<CreateModel> logger)
    {
        _usuariosClient = usuariosClient;
        _logger = logger;
    }

    // Mensajes iguales a los del MS Usuarios, que vuelve a validar todo (pruebas del 22/11).
    [BindProperty]
    [Display(Name = "Nombre")]
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    public string Nombre { get; set; } = string.Empty;

    [BindProperty]
    [Display(Name = "Apellido paterno")]
    [Required(ErrorMessage = "El apellido paterno es obligatorio.")]
    public string Apellido { get; set; } = string.Empty;

    [BindProperty]
    [Display(Name = "Apellido materno")]
    public string ApellidoMaterno { get; set; } = string.Empty;

    [BindProperty]
    [Display(Name = "Correo electrónico")]
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido.")]
    public string Correo { get; set; } = string.Empty;

    [BindProperty]
    [Display(Name = "Rol")]
    [Required(ErrorMessage = "Debe seleccionar un rol válido.")]
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

        if (!string.IsNullOrWhiteSpace(Rol) && !Roles.Any(rol =>
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
                        Rol,
                        ApellidoMaterno));

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
                "La cuenta permanece pendiente hasta configurar el primer ingreso.";

            return Page();
        }
        catch (ApiClientException error)
        {
            var seMostroError = false;

            if (error.Errors is not null)
            {
                foreach (var (campo, mensajes) in error.Errors)
                {
                    // La API usa nombres en inglés y la página, propiedades en español.
                    var campoVista = campo switch
                    {
                        "FirstName" => nameof(Nombre),
                        "LastName" => nameof(Apellido),
                        "MaternalLastName" => nameof(ApellidoMaterno),
                        "Email" => nameof(Correo),
                        "Role" => nameof(Rol),
                        _ => string.Empty
                    };

                    foreach (var mensaje in mensajes)
                    {
                        if (string.IsNullOrWhiteSpace(mensaje)) continue;

                        ModelState.AddModelError(campoVista, mensaje);
                        seMostroError = true;
                    }
                }
            }

            if (!seMostroError)
            {
                ModelState.AddModelError(string.Empty, error.Message);
            }

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
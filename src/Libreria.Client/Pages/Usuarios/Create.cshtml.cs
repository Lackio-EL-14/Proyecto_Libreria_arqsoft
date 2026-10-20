using Libreria.Application.Models;
using Libreria.Application.Ports.Primary;
using Libreria.Application.Ports.Secondary;
using Libreria.Client.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Libreria.Client.Pages.Usuarios;

[Authorize(Policy = PoliticasAcceso.SoloAdministrador)]
public class CreateModel : PageModel
{
    private readonly IGeneradorNombreUsuario _generadorNombreUsuario;
    private readonly IUsuarioService _usuarioService;
    private readonly IRolRepository _rolRepository;
    private readonly ILogger<CreateModel> _logger;
    private readonly bool _correoLocal;

    public CreateModel(
        IGeneradorNombreUsuario generadorNombreUsuario,
        IUsuarioService usuarioService,
        IRolRepository rolRepository,
        ILogger<CreateModel> logger,
        IHostEnvironment environment,
        IConfiguration configuration)
    {
        _generadorNombreUsuario = generadorNombreUsuario;
        _usuarioService = usuarioService;
        _rolRepository = rolRepository;
        _logger = logger;
        _correoLocal = environment.IsDevelopment() &&
            string.Equals(configuration["EMAIL_DELIVERY_MODE"], "PickupDirectory", StringComparison.OrdinalIgnoreCase);
    }

    [BindProperty]
    public string Nombre { get; set; } = string.Empty;

    [BindProperty]
    public string Apellido { get; set; } = string.Empty;

    [BindProperty]
    public string Correo { get; set; } = string.Empty;

    [BindProperty]
    public int RolId { get; set; }

    public string NombreUsuario { get; private set; } = string.Empty;

    public string MensajeExito { get; private set; } = string.Empty;

    public IReadOnlyList<SelectListItem> Roles { get; private set; } = [];

    public async Task OnGetAsync()
    {
        await CargarRolesAsync();
    }

    public async Task<IActionResult> OnPostGenerarAsync()
    {
        await CargarRolesAsync();

        ValidarNombreYApellido();

        if (!ModelState.IsValid)
            return Page();

        NombreUsuario =
            await _generadorNombreUsuario.GenerarAsync(
                Nombre,
                Apellido);

        return Page();
    }

    public async Task<IActionResult> OnPostCrearAsync()
    {
        await CargarRolesAsync();

        if (!Roles.Any(rol => rol.Value == RolId.ToString()))
            ModelState.AddModelError(nameof(RolId), "Debe seleccionar un rol válido.");

        if (!ModelState.IsValid)
            return Page();

        try
        {
            var input = new UsuarioAltaInput
            {
                Nombre = Nombre,
                Apellido = Apellido,
                Correo = Correo,
                RolId = RolId
            };

            NombreUsuario =
                await _usuarioService.CrearPendienteAsync(input);
            MensajeExito =
                $"Usuario {NombreUsuario} creado correctamente. " +
                (_correoLocal
                    ? "El correo de activación se guardó localmente para pruebas; no se envió por Internet."
                    : "Se envió el enlace de activación al correo registrado.");

            return Page();
        }
        catch (ArgumentException error)
        {
            ModelState.AddModelError(string.Empty, error.Message);
            return Page();
        }
        catch (InvalidOperationException error)
        {
            _logger.LogError(error, "No fue posible completar el alta de usuario.");
            ModelState.AddModelError(
                string.Empty,
                "No fue posible crear el usuario. Intente nuevamente.");

            return Page();
        }
    }

    private void ValidarNombreYApellido()
    {
        if (string.IsNullOrWhiteSpace(Nombre))
            ModelState.AddModelError(
                nameof(Nombre),
                "El nombre es obligatorio.");

        if (string.IsNullOrWhiteSpace(Apellido))
            ModelState.AddModelError(
                nameof(Apellido),
                "El apellido es obligatorio.");

        if (!string.IsNullOrWhiteSpace(Nombre) && !Nombre.Trim().All(char.IsLetter))
            ModelState.AddModelError(nameof(Nombre), "El nombre solo permite letras, incluyendo acentos y ñ.");

        if (!string.IsNullOrWhiteSpace(Apellido) && !Apellido.Trim().All(char.IsLetter))
            ModelState.AddModelError(nameof(Apellido), "El apellido solo permite letras, incluyendo acentos y ñ.");
    }

    private async Task CargarRolesAsync()
    {
        var roles = await _rolRepository.ObtenerActivosAsync();

        Roles = roles
            .Select(rol => new SelectListItem
            {
                Value = rol.RolId.ToString(),
                Text = rol.Nombre
            })
            .ToList();
    }
}

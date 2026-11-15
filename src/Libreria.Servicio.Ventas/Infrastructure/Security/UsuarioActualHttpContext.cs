using System.Security.Claims;
using Libreria.Servicio.Ventas.Application.Ports.Secondary;

namespace Libreria.Servicio.Ventas.Infrastructure.Security;

/// <summary>
/// Lee la identidad del JWT actual. Mientras US-125 no esté integrado usa únicamente
/// el usuario configurado de desarrollo como compatibilidad temporal.
/// </summary>
public sealed class UsuarioActualHttpContext : IUsuarioActual
{
    public const string SeccionDesarrollo = "UsuarioDesarrollo";
    private const string ClaimIniciales = "iniciales";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public UsuarioActualHttpContext(
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
        _environment = environment;
    }

    public Guid? UsuarioId =>
        LeerGuid(ClaimTypes.NameIdentifier)
        ?? LeerGuid("sub")
        ?? LeerGuidDesarrollo();

    public string? NombreUsuario =>
        LeerClaim(ClaimTypes.Name)
        ?? LeerClaim("name")
        ?? LeerDesarrollo("Nombre");

    public string? Iniciales =>
        LeerClaim(ClaimIniciales)
        ?? LeerDesarrollo("Iniciales");

    private string? LeerClaim(string tipo) =>
        _httpContextAccessor.HttpContext?.User.FindFirstValue(tipo);

    private Guid? LeerGuid(string tipo) =>
        Guid.TryParse(LeerClaim(tipo), out var publicId) ? publicId : null;

    private Guid? LeerGuidDesarrollo() =>
        Guid.TryParse(LeerDesarrollo("PublicId"), out var publicId) ? publicId : null;

    private string? LeerDesarrollo(string nombre) =>
        _environment.IsDevelopment()
            ? _configuration[$"{SeccionDesarrollo}:{nombre}"]
            : null;
}

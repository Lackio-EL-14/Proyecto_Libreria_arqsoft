using Libreria.Servicio.Clientes.Application.Ports.Secondary;

namespace Libreria.Servicio.Clientes.Infrastructure.Seguridad;

/// <summary>Identidad de auditoria obtenida exclusivamente del JWT validado.</summary>
public sealed class UsuarioActualDesdeToken(IHttpContextAccessor accessor) : IUsuarioActual
{
    private System.Security.Claims.ClaimsPrincipal? Principal =>
        accessor.HttpContext?.User is { Identity.IsAuthenticated: true } user ? user : null;

    public Guid? UsuarioId => Guid.TryParse(Principal?.FindFirst("sub")?.Value, out var id)
        && id != Guid.Empty ? id : null;
    public string? NombreUsuario => Principal?.FindFirst("preferred_username")?.Value;
    public string? Iniciales => string.IsNullOrWhiteSpace(NombreUsuario) ? null
        : new string(NombreUsuario.Split(new[] { ' ', '.', '_', '-' }, StringSplitOptions.RemoveEmptyEntries)
            .Take(2).Select(p => char.ToUpperInvariant(p[0])).ToArray());
}
using Libreria.Servicio.Usuarios.Application.Ports.Secondary;

namespace Libreria.Servicio.Usuarios.Infrastructure.Seguridad;

/// <summary>
/// Adaptador de <see cref="ICurrentUser"/> que lee la identidad del JWT de Keycloak ya validado
/// por <c>AddSeguridadJwt</c> (US-126): <c>sub</c> → <see cref="UserId"/> (el <c>KeycloakId</c>) y
/// <c>preferred_username</c> → <see cref="Username"/>. Reemplaza a <c>CurrentUserMiddleware</c>,
/// que confiaba en las cabeceras <c>X-Usuario-*</c>.
/// </summary>
public sealed class UsuarioActualDesdeToken : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UsuarioActualDesdeToken(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId =>
        Guid.TryParse(Principal?.FindFirst("sub")?.Value, out var sub) && sub != Guid.Empty ? sub : null;

    public string? Username => Principal?.FindFirst("preferred_username")?.Value;

    private System.Security.Claims.ClaimsPrincipal? Principal =>
        _httpContextAccessor.HttpContext?.User is { Identity.IsAuthenticated: true } usuario ? usuario : null;
}

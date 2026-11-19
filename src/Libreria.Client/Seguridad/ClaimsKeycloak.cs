using System.Security.Claims;

namespace Libreria.Client.Seguridad;

/// <summary>
/// Nombres de los claims del token de Keycloak (docs/contratos/keycloak.md §3). Con
/// <c>MapInboundClaims = false</c> llegan tal cual, sin traducirse a <see cref="ClaimTypes"/>.
/// </summary>
public static class ClaimsKeycloak
{
    public const string Sub = "sub";
    public const string NombreUsuario = "preferred_username";
    public const string Nombre = "name";
    public const string Correo = "email";
    public const string Roles = "roles";

    /// <summary>Id del usuario en Keycloak (el <c>sub</c>), o <c>null</c> si no es un GUID.</summary>
    public static Guid? ObtenerSub(this ClaimsPrincipal? principal) =>
        Guid.TryParse(principal?.FindFirst(Sub)?.Value, out var sub) && sub != Guid.Empty ? sub : null;

    /// <summary>Nombre para mostrar: <c>name</c> o, si falta, el nombre de usuario.</summary>
    public static string NombreVisible(this ClaimsPrincipal? principal) =>
        principal?.FindFirst(Nombre)?.Value ?? principal?.Identity?.Name ?? string.Empty;

    /// <summary>Roles del usuario separados por coma (para el encabezado).</summary>
    public static string RolesVisibles(this ClaimsPrincipal? principal) =>
        string.Join(", ", principal?.FindAll(Roles).Select(c => c.Value) ?? []);
}

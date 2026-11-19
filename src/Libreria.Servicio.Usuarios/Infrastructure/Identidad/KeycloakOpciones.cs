namespace Libreria.Servicio.Usuarios.Infrastructure.Identidad;

/// <summary>
/// Sección <c>Keycloak</c> de la configuración (contrato <c>docs/contratos/keycloak.md</c>).
/// Los secretos llegan solo por variable de entorno o user-secrets:
/// <c>Keycloak__ClientSecret</c> y <c>Keycloak__VerificacionClientSecret</c>.
/// </summary>
public sealed class KeycloakOpciones
{
    public const string Seccion = "Keycloak";

    /// <summary>Por ejemplo <c>http://localhost:8080/realms/libreria</c>.</summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>Cliente con cuenta de servicio para la API de administración.</summary>
    public string AdminClientId { get; set; } = "libreria-usuarios-admin";

    public string? ClientSecret { get; set; }

    /// <summary>Cliente que solo sirve para comprobar la contraseña actual (direct grant).</summary>
    public string VerificacionClientId { get; set; } = "libreria-usuarios-verificacion";

    public string? VerificacionClientSecret { get; set; }

    internal Uri UrlToken => new($"{AuthorityNormalizada}/protocol/openid-connect/token");

    internal Uri UrlCierreSesion => new($"{AuthorityNormalizada}/protocol/openid-connect/logout");

    /// <summary><c>{servidor}/admin/realms/{realm}</c>, derivado de <see cref="Authority"/>.</summary>
    internal string UrlAdministracion
    {
        get
        {
            var authority = AuthorityNormalizada;
            var indice = authority.LastIndexOf("/realms/", StringComparison.Ordinal);
            if (indice < 0)
            {
                throw new InvalidOperationException(
                    "Keycloak:Authority debe tener la forma http(s)://servidor/realms/<realm>.");
            }

            return authority[..indice] + "/admin" + authority[indice..];
        }
    }

    private string AuthorityNormalizada
    {
        get
        {
            if (!Uri.TryCreate(Authority?.Trim(), UriKind.Absolute, out var uri))
            {
                throw new InvalidOperationException("Falta Keycloak:Authority o no es una URL válida.");
            }

            return uri.ToString().TrimEnd('/');
        }
    }
}

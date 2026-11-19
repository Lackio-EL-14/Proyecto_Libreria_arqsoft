namespace Libreria.Servicio.Usuarios.Infrastructure.Identidad;

/// <summary>
/// Textos en español que el puerto de identidad devuelve en sus <c>Result</c>. Las
/// descripciones propias de Keycloak no se muestran: tienen errores de concordancia
/// («al menos 1 caracteres») y restarían puntos de ortografía.
/// </summary>
public static class MensajesIdentidad
{
    public const string ContrasenaActualIncorrecta = "La contraseña actual es incorrecta.";
    public const string CambioPendiente = "La cuenta tiene pendiente el cambio de su contraseña temporal.";
    public const string CuentaDeshabilitada = "La cuenta está deshabilitada.";
    public const string UsuarioNoEncontrado = "El usuario no existe en el proveedor de identidad.";
    public const string UsuarioDuplicado = "Ya existe un usuario con ese nombre de usuario o correo.";
    public const string RolInvalido = "Debe seleccionar un rol válido.";
    public const string ContrasenaRechazada = "La contraseña no cumple la política de seguridad.";

    /// <summary>Claves de error de la política de contraseñas de Keycloak → mensaje por regla.</summary>
    private static readonly IReadOnlyDictionary<string, string> Politica = new Dictionary<string, string>
    {
        ["invalidPasswordMinLengthMessage"] = "La contraseña debe tener al menos 8 caracteres.",
        ["invalidPasswordMinDigitsMessage"] = "La contraseña debe contener al menos un número.",
        ["invalidPasswordMinUpperCaseCharsMessage"] = "La contraseña debe contener al menos una letra mayúscula.",
        ["invalidPasswordMinLowerCaseCharsMessage"] = "La contraseña debe contener al menos una letra minúscula.",
        ["invalidPasswordMinSpecialCharsMessage"] = "La contraseña debe contener al menos un carácter especial.",
        ["invalidPasswordNotUsernameMessage"] = "La contraseña no puede ser igual al nombre de usuario.",
        ["invalidPasswordNotEmailMessage"] = "La contraseña no puede ser igual al correo.",
        ["invalidPasswordHistoryMessage"] = "La contraseña no puede repetir una de las anteriores.",
        ["invalidPasswordMaxLengthMessage"] = "La contraseña es demasiado larga."
    };

    public static string DePolitica(string? claveKeycloak) =>
        claveKeycloak is not null && Politica.TryGetValue(claveKeycloak, out var mensaje)
            ? mensaje
            : ContrasenaRechazada;
}

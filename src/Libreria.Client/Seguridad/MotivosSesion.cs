namespace Libreria.Client.Seguridad;

/// <summary>
/// Por qué se llega a <c>/Cuenta/Login</c> sin sesión. Con un motivo, la página muestra el
/// mensaje y espera a que el usuario pulse «Iniciar sesión» (en lugar de redirigir sola a
/// Keycloak), así un rechazo nunca se convierte en un bucle de redirecciones.
/// </summary>
public static class MotivosSesion
{
    public const string Vencida = "vencida";
    public const string Cerrada = "cerrada";
    public const string Inactiva = "inactiva";
    public const string NoDisponible = "no-disponible";
    public const string Error = "error";
    public const string ContrasenaCambiada = "contrasena-cambiada";

    /// <summary>Llega desde el enlace del correo de bienvenida (US-130/US-131).</summary>
    public const string PrimerIngreso = "primer-ingreso";

    /// <summary>Clave de <c>HttpContext.Items</c> con el motivo del rechazo de la sesión en esta petición.</summary>
    public const string ClaveItem = "Libreria.MotivoSesion";

    public static string? Mensaje(string? motivo) => motivo switch
    {
        Vencida => "Su sesión venció o dejó de ser válida. Inicie sesión de nuevo.",
        Cerrada => "Cerró su sesión correctamente.",
        Inactiva => "Su cuenta está inactiva. Si cree que es un error, comuníquese con el administrador.",
        NoDisponible => "No se pudo validar su sesión porque el servicio de Usuarios no responde. Intente de nuevo en unos minutos.",
        Error => "No se pudo completar el inicio de sesión. Intente de nuevo.",
        ContrasenaCambiada => "Su contraseña se cambió correctamente. Inicie sesión con la nueva contraseña.",
        PrimerIngreso => "Ingrese con el usuario y la contraseña temporal que recibió por correo. Antes de continuar, el sistema le pedirá cambiarla.",
        _ => null
    };

    public static string UrlLogin(string motivo) => $"/Cuenta/Login?motivo={Uri.EscapeDataString(motivo)}";
}

using Libreria.Servicio.Usuarios.Domain;

namespace Libreria.Servicio.Usuarios.Application.Ports.Secondary;

/// <summary>
/// Datos que el proveedor de identidad necesita para dar de alta a un usuario.
/// La contraseña no viaja aquí: se fija aparte con <see cref="IProveedorIdentidad.FijarContrasenaAsync"/>.
/// </summary>
public sealed record UsuarioIdentidad(
    string NombreUsuario,
    string Correo,
    string Nombres,
    string Apellidos);

/// <summary>
/// Puerto secundario hacia el proveedor de identidad (US-124). Keycloak guarda las
/// credenciales, los roles y las sesiones; el MS Usuarios solo guarda el perfil y el
/// <c>KeycloakId</c> (el <c>sub</c> del token).
/// </summary>
/// <remarks>
/// <para>Adaptadores: <c>KeycloakProveedorIdentidad</c> (API de administración, cliente
/// <c>libreria-usuarios-admin</c>) y <c>ProveedorIdentidadEnMemoria</c> (pruebas y
/// desarrollo sin Keycloak, con <c>ProveedorIdentidad:Modo = EnMemoria</c>).</para>
/// <para>Contrato de errores: lo previsible (política de contraseñas, duplicados, usuario
/// inexistente, contraseña incorrecta) vuelve como <see cref="Result"/> con un mensaje en
/// español. Lo inesperado (Keycloak caído, credenciales del cliente mal configuradas)
/// lanza una excepción.</para>
/// </remarks>
public interface IProveedorIdentidad
{
    /// <summary>Crea el usuario habilitado y sin contraseña. Devuelve su id en el proveedor (el <c>sub</c>).</summary>
    /// <returns>Conflicto si ya existe el nombre de usuario o el correo.</returns>
    Task<Result<Guid>> CrearUsuarioAsync(UsuarioIdentidad usuario, CancellationToken cancellationToken = default);

    /// <summary>Asigna un rol del realm (<c>Administrador</c> o <c>Vendedor</c>). Si ya lo tiene, no hace nada.</summary>
    Task<Result> AsignarRolAsync(Guid idUsuario, string rol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fija la contraseña. Con <paramref name="temporal"/> = <c>true</c>, el proveedor exige
    /// <c>UPDATE_PASSWORD</c> y no emite el token hasta que el usuario la cambia (US-130 y US-131).
    /// </summary>
    /// <returns>Error con un mensaje en español por regla incumplida, si la política la rechaza.</returns>
    Task<Result> FijarContrasenaAsync(Guid idUsuario, string contrasena, bool temporal, CancellationToken cancellationToken = default);

    /// <summary>Comprueba la contraseña actual del usuario (US-129) sin abrir una sesión duradera.</summary>
    /// <returns>Error «La contraseña actual es incorrecta.» si no coincide.</returns>
    Task<Result> VerificarContrasenaAsync(string nombreUsuario, string contrasena, CancellationToken cancellationToken = default);

    /// <summary>Habilita el inicio de sesión del usuario.</summary>
    Task<Result> HabilitarAsync(Guid idUsuario, CancellationToken cancellationToken = default);

    /// <summary>Deshabilita el inicio de sesión y cierra las sesiones abiertas del usuario (US-128).</summary>
    Task<Result> DeshabilitarAsync(Guid idUsuario, CancellationToken cancellationToken = default);

    /// <summary>Indica si el usuario todavía tiene pendiente el cambio de su contraseña temporal (estado «Pendiente», US-128).</summary>
    Task<Result<bool>> TieneCambioDeContrasenaPendienteAsync(Guid idUsuario, CancellationToken cancellationToken = default);

    /// <summary>Elimina al usuario. Sirve para compensar un alta a medias (US-130). Si ya no existe, se considera éxito.</summary>
    Task<Result> EliminarUsuarioAsync(Guid idUsuario, CancellationToken cancellationToken = default);
}

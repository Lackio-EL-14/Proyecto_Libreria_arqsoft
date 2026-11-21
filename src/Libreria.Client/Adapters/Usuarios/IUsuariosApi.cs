using Libreria.Client.Clients.Common;
using Libreria.Client.DTOs.Usuarios;

namespace Libreria.Client.Adapters.Usuarios;

/// <summary>
/// Patrón Adapter (US-140), interfaz destino: el puente de la UI hacia el MS Usuarios
/// (ruta <c>api/usuarios</c> del gateway). Las páginas dependen solo de esta interfaz.
/// Si el MS no responde lanza <see cref="ServicioNoDisponibleException"/>; si rechaza la
/// solicitud lanza <see cref="ApiClientException"/> con el mensaje del <c>ProblemDetails</c>.
/// </summary>
public interface IUsuariosApi
{
    /// <summary>Si el usuario (por su <c>sub</c> de Keycloak) sigue activo en el MS Usuarios (US-125).</summary>
    Task<bool> SesionActivaAsync(
        Guid keycloakId,
        CancellationToken cancellationToken = default);

    /// <summary>Contrato 6.4: nombres por <c>PublicId</c> (GUID) para la auditoría.</summary>
    Task<IReadOnlyDictionary<Guid, string>> ObtenerNombresAsync(
        IEnumerable<Guid> usuarioIds,
        CancellationToken cancellationToken = default);

    /// <summary>Listado sin contraseñas; el MS filtra por estado y ordena (orden = nombre, usuario, correo, rol o estado).</summary>
    Task<IReadOnlyList<UserListItemDto>> ObtenerUsuariosAsync(
        string? estado = null,
        string? orden = null,
        bool descendente = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> ObtenerRolesAsync(
        CancellationToken cancellationToken = default);

    Task<CreateUserResultDto?> CrearUsuarioAsync(
        CreateUserRequestDto request,
        CancellationToken cancellationToken = default);

    Task CambiarContrasenaAsync(
        ChangePasswordRequestDto request,
        CancellationToken cancellationToken = default);

    Task DarDeBajaUsuarioAsync(
        Guid publicId,
        CancellationToken cancellationToken = default);
}

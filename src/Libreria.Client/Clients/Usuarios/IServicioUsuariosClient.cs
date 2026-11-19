using Libreria.Client.Clients.Common;
using Libreria.Client.DTOs.Usuarios;

namespace Libreria.Client.Clients.Usuarios;

public interface IServicioUsuariosClient : IApiClient
{
    Task<bool> SesionActivaAsync(
        int usuarioId,
        CancellationToken cancellationToken = default);

    /// <summary>Si el usuario (por su <c>sub</c> de Keycloak) sigue activo en el MS Usuarios (US-125).</summary>
    Task<bool> SesionActivaAsync(
        Guid keycloakId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, string>> ObtenerNombresAsync(
        IEnumerable<int> usuarioIds,
        CancellationToken cancellationToken = default);

    // Contrato 6.4: consulta de nombres por PublicId (GUID) para auditoría entre MS.
    Task<IReadOnlyDictionary<Guid, string>> ObtenerNombresAsync(
        IEnumerable<Guid> usuarioIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserListItemDto>> ObtenerUsuariosAsync(
    string? estado = null,
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

using Libreria.Client.Clients.Common;
using Libreria.Client.DTOs.Usuarios;

namespace Libreria.Client.Clients.Usuarios;

public interface IServicioUsuariosClient : IApiClient
{
    Task<AuthenticatedUserDto?> IniciarSesionAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default);

    Task<bool> SesionActivaAsync(
        int usuarioId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, string>> ObtenerNombresAsync(
        IEnumerable<int> usuarioIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserListItemDto>> ObtenerUsuariosAsync(
    string? estado = null,
    CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> ObtenerRolesAsync(
    CancellationToken cancellationToken = default);

    Task<CreateUserResultDto?> CrearUsuarioAsync(
        CreateUserRequestDto request,
        CancellationToken cancellationToken = default);

    Task ActivarUsuarioAsync(
    ActivateUserRequestDto request,
    CancellationToken cancellationToken = default);

    Task CambiarContrasenaAsync(
    ChangePasswordRequestDto request,
    CancellationToken cancellationToken = default);

    Task DarDeBajaUsuarioAsync(
    Guid publicId,
    CancellationToken cancellationToken = default);

    Task ReenviarActivacionAsync(
        Guid publicId,
        CancellationToken cancellationToken = default);
}

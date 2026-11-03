using Libreria.Client.Clients.Common;
using Libreria.Client.DTOs.Usuarios;

namespace Libreria.Client.Clients.Usuarios;

public sealed class ServicioUsuariosClient : ApiClientBase, IServicioUsuariosClient
{
    public ServicioUsuariosClient(HttpClient httpClient)
        : base(httpClient, "Usuarios")
    {
    }

    public Task<AuthenticatedUserDto?> IniciarSesionAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default) =>
        PostAsync<LoginRequestDto, AuthenticatedUserDto>(
            "api/auth/login",
            request,
            cancellationToken);

    public async Task<bool> SesionActivaAsync(
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        var result = await GetAsync<bool>(
            $"api/usuarios/{usuarioId}/sesion-activa",
            cancellationToken);

        return result;
    }

    public async Task<IReadOnlyDictionary<int, string>> ObtenerNombresAsync(
        IEnumerable<int> usuarioIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(usuarioIds);

        var ids = usuarioIds
            .Where(id => id > 0)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<int, string>();
        }

        var query = string.Join(
            "&",
            ids.Select(id => $"ids={id}"));

        return await GetAsync<Dictionary<int, string>>(
                   $"api/usuarios/nombres?{query}",
                   cancellationToken)
               ?? new Dictionary<int, string>();
    }

    public async Task<IReadOnlyList<UserListItemDto>> ObtenerUsuariosAsync(
    string? estado = null,
    CancellationToken cancellationToken = default)
    {
        var query = string.IsNullOrWhiteSpace(estado)
            ? "api/usuarios"
            : $"api/usuarios?estado={Uri.EscapeDataString(estado.Trim())}";

        return await GetAsync<List<UserListItemDto>>(
                query,
                cancellationToken)
            ?? [];
    }

    public async Task<IReadOnlyList<string>> ObtenerRolesAsync(
        CancellationToken cancellationToken = default)
    {
        return await GetAsync<List<string>>(
                "api/usuarios/roles",
                cancellationToken)
            ?? [];
    }

    public Task<CreateUserResultDto?> CrearUsuarioAsync(
        CreateUserRequestDto request,
        CancellationToken cancellationToken = default) =>
        PostAsync<CreateUserRequestDto, CreateUserResultDto>(
            "api/usuarios",
            request,
            cancellationToken);

    public Task ActivarUsuarioAsync(
    ActivateUserRequestDto request,
    CancellationToken cancellationToken = default) =>
    PostAsync(
        "api/usuarios/activar",
        request,
        cancellationToken);

    public Task CambiarContrasenaAsync(
    ChangePasswordRequestDto request,
    CancellationToken cancellationToken = default) =>
    PutAsync(
        "api/usuarios/contrasena",
        request,
        cancellationToken);

    public Task DarDeBajaUsuarioAsync(
        Guid publicId,
        CancellationToken cancellationToken = default)
    {
        if (publicId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del usuario es obligatorio.",
                nameof(publicId));
        }

        return DeleteAsync(
            $"api/usuarios/{publicId:D}",
            cancellationToken);
    }
}

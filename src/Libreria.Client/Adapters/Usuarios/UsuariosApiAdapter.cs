using Libreria.Client.Adapters.DependencyInjection;
using Libreria.Client.Clients.Common;
using Libreria.Client.DTOs.Usuarios;

namespace Libreria.Client.Adapters.Usuarios;

/// <summary>
/// Patrón Adapter (US-140): adapta el `HttpClient` «ApiGateway» a la interfaz destino
/// <see cref="IUsuariosApi"/>. Arma la URL, envía el token (handler de US-125) y traduce JSON,
/// `ProblemDetails` y códigos HTTP a DTOs del Front y a mensajes para la UI.
/// </summary>
public sealed class UsuariosApiAdapter : ApiClientBase, IUsuariosApi
{
    public UsuariosApiAdapter(IHttpClientFactory fabrica)
        : base(fabrica.CreateClient(ApiGateway.NombreCliente), "Usuarios")
    {
    }

    public async Task<bool> SesionActivaAsync(
        Guid keycloakId,
        CancellationToken cancellationToken = default)
    {
        if (keycloakId == Guid.Empty)
        {
            return false;
        }

        return await GetAsync<bool>(
            $"api/usuarios/{keycloakId:D}/sesion-activa",
            cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, string>> ObtenerNombresAsync(
        IEnumerable<Guid> usuarioIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(usuarioIds);

        var ids = usuarioIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var query = string.Join("&", ids.Select(id => $"ids={id}"));

        try
        {
            return await GetAsync<Dictionary<Guid, string>>(
                       $"api/usuarios/nombres?{query}",
                       cancellationToken)
                   ?? new Dictionary<Guid, string>();
        }
        catch (ApiClientException)
        {
            // Los nombres son informativos: si Usuarios rechaza los GUID, la página sigue
            // y la auditoría muestra «Usuario no disponible».
            return new Dictionary<Guid, string>();
        }
    }

    public async Task<IReadOnlyList<UserListItemDto>> ObtenerUsuariosAsync(
        string? estado = null,
        string? orden = null,
        bool descendente = false,
        CancellationToken cancellationToken = default)
    {
        var parametros = new List<string>();
        if (!string.IsNullOrWhiteSpace(estado)) parametros.Add($"estado={Uri.EscapeDataString(estado.Trim())}");
        if (!string.IsNullOrWhiteSpace(orden)) parametros.Add($"orden={Uri.EscapeDataString(orden.Trim())}");
        if (descendente) parametros.Add("descendente=true");

        var query = parametros.Count == 0
            ? "api/usuarios"
            : "api/usuarios?" + string.Join("&", parametros);

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

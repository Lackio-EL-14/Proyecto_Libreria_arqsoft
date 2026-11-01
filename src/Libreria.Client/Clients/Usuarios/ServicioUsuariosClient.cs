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
}

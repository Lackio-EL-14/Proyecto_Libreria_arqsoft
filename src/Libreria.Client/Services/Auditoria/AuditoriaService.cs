using Libreria.Client.Clients.Usuarios;
using Libreria.Client.DTOs.Common;

namespace Libreria.Client.Services.Auditoria;

public sealed class AuditoriaService : IAuditoriaService
{
    private readonly IServicioUsuariosClient _usuariosClient;

    public AuditoriaService(IServicioUsuariosClient usuariosClient)
    {
        _usuariosClient = usuariosClient;
    }

    public async Task<AuditoriaRegistroDto> ObtenerAsync(
        int? usuarioCreacionId,
        DateTime? fechaCreacion,
        int? usuarioModificacionId,
        DateTime? fechaModificacion,
        int? usuarioAnulacionId = null,
        DateTime? fechaAnulacion = null,
        CancellationToken cancellationToken = default)
    {
        var ids = new[]
            {
                usuarioCreacionId,
                usuarioModificacionId,
                usuarioAnulacionId
            }
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToArray();

        var nombres = await _usuariosClient.ObtenerNombresAsync(
            ids,
            cancellationToken);

        return new AuditoriaRegistroDto(
            ResolverNombre(usuarioCreacionId, nombres),
            fechaCreacion,
            ResolverNombre(usuarioModificacionId, nombres),
            usuarioModificacionId.HasValue ? fechaModificacion : null,
            ResolverNombre(usuarioAnulacionId, nombres),
            usuarioAnulacionId.HasValue ? fechaAnulacion : null);
    }

    private static string? ResolverNombre(
        int? usuarioId,
        IReadOnlyDictionary<int, string> nombres)
    {
        if (!usuarioId.HasValue)
        {
            return null;
        }

        return nombres.TryGetValue(usuarioId.Value, out var nombre)
            ? nombre
            : "Usuario no disponible";
    }
}

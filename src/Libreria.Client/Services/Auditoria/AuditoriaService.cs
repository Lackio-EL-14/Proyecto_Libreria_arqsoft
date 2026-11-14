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

    public async Task<AuditoriaRegistroDto> ObtenerAsync(
        Guid? usuarioCreacionId,
        DateTime? fechaCreacion,
        Guid? usuarioModificacionId,
        DateTime? fechaModificacion,
        CancellationToken cancellationToken = default)
    {
        var ids = new[] { usuarioCreacionId, usuarioModificacionId }
            .Where(id => id.HasValue)
            .Select(id => id!.Value);

        var nombres = await _usuariosClient.ObtenerNombresAsync(ids, cancellationToken);

        return new AuditoriaRegistroDto(
            ResolverNombre(usuarioCreacionId, nombres),
            fechaCreacion,
            ResolverNombre(usuarioModificacionId, nombres),
            usuarioModificacionId.HasValue ? fechaModificacion : null);
    }

    private static string? ResolverNombre<TId>(
        TId? usuarioId,
        IReadOnlyDictionary<TId, string> nombres)
        where TId : struct
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

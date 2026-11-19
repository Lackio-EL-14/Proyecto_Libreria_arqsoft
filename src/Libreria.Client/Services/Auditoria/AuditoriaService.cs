using Libreria.Client.Adapters.Usuarios;
using Libreria.Client.DTOs.Common;
using Libreria.Client.DTOs.Productos;

namespace Libreria.Client.Services.Auditoria;

public sealed class AuditoriaService : IAuditoriaService
{
    private readonly IUsuariosApi _usuariosClient;

    public AuditoriaService(IUsuariosApi usuariosClient)
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

    public async Task<IReadOnlyList<HistoricoCostoDto>>
        ResolverUsuariosHistoricoAsync(
            IReadOnlyList<HistoricoCostoDto> historial,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(historial);

        var ids = historial
            .Select(item =>
                Guid.TryParse(
                    item.RegistradoPor,
                    out var usuarioId)
                    ? usuarioId
                    : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        var nombres =
            await _usuariosClient.ObtenerNombresAsync(
                ids,
                cancellationToken);

        return historial
            .Select(item =>
            {
                if (string.IsNullOrWhiteSpace(
                        item.RegistradoPor))
                {
                    return item;
                }

                if (!Guid.TryParse(
                        item.RegistradoPor,
                        out var usuarioId))
                {
                    return item;
                }

                var nombre =
                    nombres.TryGetValue(
                        usuarioId,
                        out var nombreUsuario)
                        ? nombreUsuario
                        : "Usuario no disponible";

                return item with
                {
                    RegistradoPor = nombre
                };
            })
            .ToList();
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

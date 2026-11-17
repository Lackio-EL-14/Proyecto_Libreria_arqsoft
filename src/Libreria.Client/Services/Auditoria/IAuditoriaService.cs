using Libreria.Client.DTOs.Common;
using Libreria.Client.DTOs.Productos;

namespace Libreria.Client.Services.Auditoria;

public interface IAuditoriaService
{
    Task<AuditoriaRegistroDto> ObtenerAsync(
        int? usuarioCreacionId,
        DateTime? fechaCreacion,
        int? usuarioModificacionId,
        DateTime? fechaModificacion,
        int? usuarioAnulacionId = null,
        DateTime? fechaAnulacion = null,
        CancellationToken cancellationToken = default);

    // Para los MS cuya auditoría guarda el PublicId del usuario (Clientes, contrato 6.4).
    Task<AuditoriaRegistroDto> ObtenerAsync(
        Guid? usuarioCreacionId,
        DateTime? fechaCreacion,
        Guid? usuarioModificacionId,
        DateTime? fechaModificacion,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HistoricoCostoDto>> ResolverUsuariosHistoricoAsync(
        IReadOnlyList<HistoricoCostoDto> historial,
        CancellationToken cancellationToken = default);
}

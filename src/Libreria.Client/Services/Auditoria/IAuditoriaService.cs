using Libreria.Client.DTOs.Common;
using Libreria.Client.DTOs.Productos;

namespace Libreria.Client.Services.Auditoria;

public interface IAuditoriaService
{
    // La auditoría de los 4 MS guarda el sub de Keycloak del usuario (contrato 6.4).
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

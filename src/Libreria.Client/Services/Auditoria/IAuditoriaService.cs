using Libreria.Client.DTOs.Common;

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
}

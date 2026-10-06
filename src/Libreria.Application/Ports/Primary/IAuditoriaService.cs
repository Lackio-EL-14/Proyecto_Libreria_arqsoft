using Libreria.Application.Models;

namespace Libreria.Application.Ports.Primary;

public interface IAuditoriaService
{
    Task<AuditoriaRegistro> ObtenerAsync(
        int? usuarioCreacionId,
        DateTime? fechaCreacion,
        int? usuarioModificacionId,
        DateTime? fechaModificacion);
}

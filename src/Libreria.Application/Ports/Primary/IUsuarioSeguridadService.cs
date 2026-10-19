using Libreria.Application.Models;

namespace Libreria.Application.Ports.Primary;

public interface IUsuarioSeguridadService
{
    Task<ResultadoOperacion> DarDeBajaAsync(Guid publicId);
    Task<bool> SesionActivaAsync(int usuarioId);
}

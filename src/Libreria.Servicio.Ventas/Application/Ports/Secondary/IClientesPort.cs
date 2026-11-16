using Libreria.Servicio.Ventas.Application.Comun;
using Libreria.Servicio.Ventas.Application.Models;

namespace Libreria.Servicio.Ventas.Application.Ports.Secondary;

public interface IClientesPort
{
    Task<Result<ClienteVentaSnapshot>> ObtenerPorCiNitAsync(
        string ciNit,
        CancellationToken cancellationToken = default);
}

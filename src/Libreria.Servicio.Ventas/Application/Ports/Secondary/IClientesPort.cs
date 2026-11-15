using Libreria.Servicio.Ventas.Application.Models;

namespace Libreria.Servicio.Ventas.Application.Ports.Secondary;

public interface IClientesPort
{
    Task<ClienteVentaSnapshot?> ObtenerPorCiNitAsync(string ciNit);
}

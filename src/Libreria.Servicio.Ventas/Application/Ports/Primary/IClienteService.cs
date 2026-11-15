using Libreria.Servicio.Ventas.Application.Models;

namespace Libreria.Servicio.Ventas.Application.Ports.Primary;

public interface IClienteService
{
    Task<ClienteRegistroResult> RegistrarRapidoAsync(ClienteInput input);
}

using Libreria.Application.Models;

namespace Libreria.Application.Ports.Primary;

public interface IClienteService
{
    Task<ClienteRegistroResult> RegistrarRapidoAsync(ClienteInput input);
}

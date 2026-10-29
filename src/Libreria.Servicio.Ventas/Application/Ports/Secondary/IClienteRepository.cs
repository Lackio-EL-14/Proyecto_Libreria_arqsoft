using Libreria.Domain;

namespace Libreria.Servicio.Ventas.Application.Ports.Secondary;

public interface IClienteRepository
{
    Task<IReadOnlyList<Cliente>> BuscarPorCiNitAsync(string ciNit);
    Task<Cliente?> ObtenerPorCiNitAsync(string ciNit);
    Task<Cliente> CrearRapidoAsync(Cliente cliente);
    Task<bool> ExisteCiNitAsync(string ciNit);
}
using Libreria.Servicio.Ventas.Application.Models;
using Libreria.Servicio.Ventas.Application.Ports.Secondary;

namespace Libreria.Servicio.Ventas.Infrastructure.Adapters;

public sealed class ClientesDesarrolloAdapter : IClientesPort
{
    private static readonly IReadOnlyDictionary<string, ClienteVentaSnapshot> Clientes =
        new Dictionary<string, ClienteVentaSnapshot>(StringComparer.OrdinalIgnoreCase)
        {
            ["9999999"] = new(
                Guid.Parse("c0000000-0000-0000-0000-000000000001"),
                "9999999",
                "Universidad Mayor"),
            ["4567890"] = new(
                Guid.Parse("c0000000-0000-0000-0000-000000000002"),
                "4567890",
                "María Fernanda Quispe Choque")
        };

    public Task<ClienteVentaSnapshot?> ObtenerPorCiNitAsync(string ciNit)
    {
        Clientes.TryGetValue(ciNit.Trim(), out var cliente);
        return Task.FromResult(cliente);
    }
}

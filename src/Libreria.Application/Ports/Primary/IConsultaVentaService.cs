using Libreria.Application.Models;

namespace Libreria.Application.Ports.Primary;

public interface IConsultaVentaService
{
    Task<IReadOnlyList<ProductoVentaItem>> BuscarProductosAsync(string? nombre);
    Task<IReadOnlyList<ClienteVentaItem>> BuscarClientesAsync(string? ciNit);
}

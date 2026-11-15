using Libreria.Servicio.Ventas.Application.Models;

namespace Libreria.Servicio.Ventas.Application.Ports.Primary;

public interface IConsultaVentaService
{
    Task<IReadOnlyList<ProductoVentaItem>> BuscarProductosAsync(string? nombre);
    Task<IReadOnlyList<ClienteVentaItem>> BuscarClientesAsync(string? ciNit);
}

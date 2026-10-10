using Libreria.Application.Models;
using Libreria.Application.Ports.Primary;
using Libreria.Application.Ports.Secondary;

namespace Libreria.Application.Services;

public class ConsultaVentaService : IConsultaVentaService
{
    private readonly IProductoRepository _productos;
    private readonly IClienteRepository _clientes;

    public ConsultaVentaService(
        IProductoRepository productos,
        IClienteRepository clientes)
    {
        _productos = productos;
        _clientes = clientes;
    }

    public async Task<IReadOnlyList<ProductoVentaItem>> BuscarProductosAsync(string? nombre)
    {
        var busqueda = nombre?.Trim();
        if (string.IsNullOrEmpty(busqueda))
        {
            return Array.Empty<ProductoVentaItem>();
        }

        var productos = await _productos.ObtenerActivasAsync(busqueda);

        // El catálogo también busca por descripción. En ventas se busca solo por nombre.
        return productos
            .Where(p => p.Nombre.Contains(busqueda, StringComparison.OrdinalIgnoreCase))
            .Select(p => new ProductoVentaItem(p.PublicId, p.Nombre, p.Stock, p.PrecioVenta))
            .ToList();
    }

    public async Task<IReadOnlyList<ClienteVentaItem>> BuscarClientesAsync(string? ciNit)
    {
        var busqueda = ciNit?.Trim();
        if (string.IsNullOrEmpty(busqueda))
        {
            return Array.Empty<ClienteVentaItem>();
        }

        var clientes = await _clientes.BuscarPorCiNitAsync(busqueda);
        return clientes
            .Select(c => new ClienteVentaItem(c.CiNit, c.RazonSocial))
            .ToList();
    }
}

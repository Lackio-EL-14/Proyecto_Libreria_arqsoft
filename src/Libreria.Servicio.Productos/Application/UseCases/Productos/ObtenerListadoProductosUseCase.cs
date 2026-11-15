using Libreria.Servicio.Productos.Application.Abstractions;
using Libreria.Servicio.Productos.Application.Models.Productos;

namespace Libreria.Servicio.Productos.Application.UseCases.Productos;

public sealed class ObtenerListadoProductosUseCase
{
    private readonly IProductoRepository _repository;

    public ObtenerListadoProductosUseCase(IProductoRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<ProductoListItem>> EjecutarAsync(
        string? busqueda = null)
    {
        var productos = await _repository.ObtenerActivasAsync(busqueda);

        return productos
            .Select(producto => new ProductoListItem(
                producto.PublicId,
                producto.Nombre,
                producto.Stock,
                producto.PrecioVenta,
                producto.CostoAdquisicionActual,
                producto.Categoria.PublicId,
                producto.Categoria.Nombre,
                producto.Marca.PublicId,
                producto.Marca.Nombre,
                producto.EsPerecedero))
            .ToList();
    }
}
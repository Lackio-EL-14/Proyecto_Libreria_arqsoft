using Libreria.Servicio.Productos.Application.Abstractions;
using Libreria.Servicio.Productos.Application.Models.Productos;

namespace Libreria.Servicio.Productos.Application.UseCases.Productos;

public sealed class ObtenerListadoProductosUseCase
{
    private readonly IProductoRepository _productos;
    private readonly ICatalogoProductoRepository _catalogos;

    public ObtenerListadoProductosUseCase(
        IProductoRepository productos,
        ICatalogoProductoRepository catalogos)
    {
        _productos = productos;
        _catalogos = catalogos;
    }

    public async Task<ProductoListado> EjecutarAsync(
        string? busqueda = null,
        Guid? categoriaPublicId = null,
        Guid? marcaPublicId = null)
    {
        var productos =
            await _productos.ObtenerActivasFiltradasAsync(
                busqueda,
                categoriaPublicId,
                marcaPublicId);

        var categorias =
            await _catalogos.ObtenerCategoriasActivasAsync();

        var marcas =
            await _catalogos.ObtenerMarcasActivasAsync();

        var items = productos
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

        return new ProductoListado(
            items,
            categorias
                .Select(categoria =>
                    new CategoriaProductoOption(
                        categoria.PublicId,
                        categoria.Nombre))
                .ToList(),
            marcas
                .Select(marca =>
                    new MarcaProductoOption(
                        marca.PublicId,
                        marca.Nombre))
                .ToList());
    }
}
using Libreria.Servicio.Productos.Application.Abstractions;
using Libreria.Servicio.Productos.Application.Comun;
using Libreria.Servicio.Productos.Application.Models.Productos;

namespace Libreria.Servicio.Productos.Application.UseCases.Productos;

public sealed class ObtenerProductoEdicionUseCase
{
    private readonly IProductoRepository _productos;
    private readonly ICatalogoProductoRepository _catalogos;

    public ObtenerProductoEdicionUseCase(
        IProductoRepository productos,
        ICatalogoProductoRepository catalogos)
    {
        _productos = productos;
        _catalogos = catalogos;
    }

    public async Task<Result<ProductoEdicion>> EjecutarAsync(
        Guid publicId)
    {
        var producto =
            await _productos.ObtenerPorPublicIdAsync(
                publicId,
                true);

        if (producto is null)
        {
            return Result<ProductoEdicion>.Fallo(
                Result.NoEncontrado(
                    "El producto no existe o está inactivo."));
        }

        var categorias =
            await _catalogos.ObtenerCategoriasActivasAsync();

        var marcas =
            await _catalogos.ObtenerMarcasActivasAsync();

        var input = new ProductoInput
        {
            Nombre = producto.Nombre,
            DescripcionEspecifica =
                producto.DescripcionEspecifica,
            EsPerecedero = producto.EsPerecedero,
            FechaVencimiento = producto.FechaVencimiento,
            Stock = producto.Stock,
            PrecioVenta = producto.PrecioVenta,
            CostoAdquisicion =
                producto.CostoAdquisicionActual,
            CategoriaPublicId =
                producto.Categoria.PublicId,
            MarcaPublicId =
                producto.Marca.PublicId
        };

        return Result<ProductoEdicion>.Exito(
            new ProductoEdicion(
                input,
                categorias
                    .Select(item =>
                        new CategoriaProductoOption(
                            item.PublicId,
                            item.Nombre))
                    .ToList(),
                marcas
                    .Select(item =>
                        new MarcaProductoOption(
                            item.PublicId,
                            item.Nombre))
                    .ToList()));
    }
}
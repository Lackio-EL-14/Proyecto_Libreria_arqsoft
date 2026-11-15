using Libreria.Servicio.Productos.Application.Abstractions;
using Libreria.Servicio.Productos.Application.Models.Productos;

namespace Libreria.Servicio.Productos.Application.UseCases.Productos;

public sealed class ObtenerFormularioProductoUseCase
{
    private readonly ICatalogoProductoRepository _catalogos;

    public ObtenerFormularioProductoUseCase(
        ICatalogoProductoRepository catalogos)
    {
        _catalogos = catalogos;
    }

    public async Task<ProductoFormulario> EjecutarAsync()
    {
        var categorias =
            await _catalogos.ObtenerCategoriasActivasAsync();

        var marcas =
            await _catalogos.ObtenerMarcasActivasAsync();

        return new ProductoFormulario(
            categorias
                .Select(item => new CategoriaProductoOption(
                    item.PublicId,
                    item.Nombre))
                .ToList(),
            marcas
                .Select(item => new MarcaProductoOption(
                    item.PublicId,
                    item.Nombre))
                .ToList());
    }
}
using Libreria.Web.Pages.Productos.Models;
using Libreria.Web.Pages.Productos.Repositories;
using Libreria.Web.Data.Repositories;
using Libreria.Web.Domain.Entities;

namespace Libreria.Web.Pages.Productos.Services;

public class ProductoService : IProductoService
{
    private readonly ICatalogoProductoRepository _catalogoRepository;
    private readonly ProductoValidator _validator;

    public ProductoService(
        ICatalogoProductoRepository catalogoRepository,
        ProductoValidator validator)
    {
        _catalogoRepository = catalogoRepository;
        _validator = validator;
    }


    public async Task<ProductoListado> ObtenerListadoAsync(
    string? busqueda,
    int? categoriaId,
    int? marcaId,
    ICrudRepository<Producto> repository)
    {
        var productos = await repository.ObtenerActivasAsync(busqueda);

        var categorias = _catalogoRepository.ObtenerCategoriasActivas();
        var marcas = _catalogoRepository.ObtenerMarcasActivas();

        var productosFiltrados = productos
            .Where(p => !categoriaId.HasValue || p.CategoriaId == categoriaId.Value)
            .Where(p => !marcaId.HasValue || p.MarcaId == marcaId.Value)
            .Select(p => new ProductoListItem(
                p.PublicId,
                p.Nombre,
                p.Stock,
                p.PrecioVenta,
                p.CostoAdquisicionActual,
                categorias.FirstOrDefault(c => c.CategoriaId == p.CategoriaId)?.Nombre ?? string.Empty,
                marcas.FirstOrDefault(m => m.MarcaId == p.MarcaId)?.Nombre ?? string.Empty
                ,p.EsPerecedero))
            .ToList();

        return new ProductoListado(
            productosFiltrados,
            categorias,
            marcas);
    }

    public ProductoFormulario ObtenerFormulario()
    {
        return new ProductoFormulario(
            _catalogoRepository.ObtenerCategoriasActivas(),
            _catalogoRepository.ObtenerMarcasActivas());
    }

    public async Task<ResultadoOperacion> RegistrarAsync(
      ProductoInput input,
      ICrudRepository<Producto> repository)
    {
        _validator.Normalizar(input);

        var errores = _validator.Validar(input);
        if (errores.Count > 0)
        {
            return ResultadoOperacion.Invalido(errores);
        }

        var producto = new Producto
        {
            Nombre = input.Nombre,
            DescripcionEspecifica = input.DescripcionEspecifica,
            EsPerecedero = input.EsPerecedero,
            FechaVencimiento = input.FechaVencimiento,
            Stock = input.Stock,
            PrecioVenta = input.PrecioVenta,
            CostoAdquisicionActual = input.CostoAdquisicion,
            CategoriaId = input.CategoriaId,
            MarcaId = input.MarcaId
        };

        await repository.CrearAsync(producto);

        return ResultadoOperacion.Correcto();
    }

    public async Task<ProductoDetalle?> ObtenerDetalleAsync(
    Guid publicId,
    ICrudRepository<Producto> repository)
    {
        var producto = await repository.ObtenerPorPublicIdAsync(publicId, true);

        if (producto is null)
        {
            return null;
        }

        var categorias = _catalogoRepository.ObtenerCategoriasActivas();
        var marcas = _catalogoRepository.ObtenerMarcasActivas();

        var nombreCategoria = categorias
            .FirstOrDefault(c => c.CategoriaId == producto.CategoriaId)?.Nombre ?? string.Empty;

        var nombreMarca = marcas
            .FirstOrDefault(m => m.MarcaId == producto.MarcaId)?.Nombre ?? string.Empty;

        return new ProductoDetalle(
            producto.ProductoId,
            producto.Nombre,
            producto.DescripcionEspecifica,
            producto.EsPerecedero,
            producto.FechaVencimiento,
            producto.Stock,
            producto.PrecioVenta,
            producto.CostoAdquisicionActual,
            producto.CategoriaId,
            nombreCategoria,
            producto.MarcaId,
            nombreMarca);
    }

    public async Task<ProductoEdicion?> ObtenerEdicionAsync(
    Guid publicId,
    ICrudRepository<Producto> repository)
    {
        var producto = await repository.ObtenerPorPublicIdAsync(publicId, true);

        if (producto is null)
        {
            return null;
        }

        var input = new ProductoInput
        {
            Nombre = producto.Nombre,
            DescripcionEspecifica = producto.DescripcionEspecifica,
            EsPerecedero = producto.EsPerecedero,
            FechaVencimiento = producto.FechaVencimiento,
            Stock = producto.Stock,
            PrecioVenta = producto.PrecioVenta,
            CostoAdquisicion = producto.CostoAdquisicionActual,
            CategoriaId = producto.CategoriaId,
            MarcaId = producto.MarcaId
        };

        return new ProductoEdicion(
            producto.ProductoId,
            input,
            _catalogoRepository.ObtenerCategoriasActivas(),
            _catalogoRepository.ObtenerMarcasActivas());
    }

    public async Task<ResultadoOperacion> ActualizarAsync(
    Guid publicId,
    ProductoInput input,
    ICrudRepository<Producto> repository)
    {
        var producto = await repository.ObtenerPorPublicIdAsync(publicId, true);

        if (producto is null)
        {
            return ResultadoOperacion.NoExiste();
        }

        _validator.Normalizar(input);

        var errores = _validator.Validar(input);
        if (errores.Count > 0)
        {
            return ResultadoOperacion.Invalido(errores);
        }

        producto.Nombre = input.Nombre;
        producto.DescripcionEspecifica = input.DescripcionEspecifica;
        producto.EsPerecedero = input.EsPerecedero;
        producto.FechaVencimiento = input.FechaVencimiento;
        producto.Stock = input.Stock;
        producto.PrecioVenta = input.PrecioVenta;
        producto.CostoAdquisicionActual = input.CostoAdquisicion;
        producto.CategoriaId = input.CategoriaId;
        producto.MarcaId = input.MarcaId;

        return await repository.ActualizarAsync(producto)
            ? ResultadoOperacion.Correcto()
            : ResultadoOperacion.NoExiste();
    }

    public async Task<ProductoDetalle?> ObtenerParaBajaAsync(
    Guid publicId,
    ICrudRepository<Producto> repository)
    {
        return await ObtenerDetalleAsync(publicId, repository);
    }

    public async Task<bool> DarDeBajaAsync(
    Guid publicId,
    ICrudRepository<Producto> repository)
    {
        return await repository.CambiarEstadoAsync(publicId, false);
    }
}

using Libreria.Web.Pages.Productos.Models;
using Libreria.Web.Pages.Productos.Repositories;

namespace Libreria.Web.Pages.Productos.Services;

public class ProductoService :
    IConsultaProductosService,
    IRegistroProductoService,
    IEdicionProductoService,
    IConsultaProductoDetalleService,
    IBajaProductoService
{
    private readonly IProductoRepository _productoRepository;
    private readonly IConsultaCatalogoProductoRepository _catalogoRepository;
    private readonly ProductoValidator _validator;

    public ProductoService(
        IProductoRepository productoRepository,
        IConsultaCatalogoProductoRepository catalogoRepository,
        ProductoValidator validator)
    {
        _productoRepository = productoRepository;
        _catalogoRepository = catalogoRepository;
        _validator = validator;
    }

    public ProductoListado ObtenerListado(
        string? busqueda,
        int? categoriaId,
        int? marcaId)
    {
        return new ProductoListado(
            _productoRepository.ObtenerProductos(busqueda, categoriaId, marcaId),
            _catalogoRepository.ObtenerCategoriasActivas(),
            _catalogoRepository.ObtenerMarcasActivas());
    }

    public ProductoFormulario ObtenerFormulario()
    {
        return new ProductoFormulario(
            _catalogoRepository.ObtenerCategoriasActivas(),
            _catalogoRepository.ObtenerMarcasActivas());
    }

    public ResultadoOperacion Registrar(ProductoInput input)
    {
        _validator.Normalizar(input);
        var errores = _validator.Validar(input);
        if (errores.Count > 0)
        {
            return ResultadoOperacion.Invalido(errores);
        }

        _productoRepository.CrearConHistorico(input);
        return ResultadoOperacion.Correcto();
    }

    public ProductoEdicion? ObtenerEdicion(int productoId)
    {
        var producto = _productoRepository.ObtenerActivoPorId(productoId);
        if (producto is null)
        {
            return null;
        }

        return new ProductoEdicion(
            producto.ProductoId,
            CrearInputEdicion(producto),
            _catalogoRepository.ObtenerCategoriasActivas(),
            _catalogoRepository.ObtenerMarcasActivas());
    }

    public ResultadoOperacion Actualizar(int productoId, ProductoInput input)
    {
        if (_productoRepository.ObtenerActivoPorId(productoId) is null)
        {
            return ResultadoOperacion.NoExiste();
        }

        _validator.Normalizar(input);
        var errores = _validator.Validar(input);
        if (errores.Count > 0)
        {
            return ResultadoOperacion.Invalido(errores);
        }

        return _productoRepository.ActualizarConHistorico(
                productoId,
                input,
                "Edición manual")
            ? ResultadoOperacion.Correcto()
            : ResultadoOperacion.NoExiste();
    }

    public ProductoDetalle? ObtenerDetalle(int productoId)
    {
        return _productoRepository.ObtenerActivoPorId(productoId);
    }

    public ProductoDetalle? ObtenerParaBaja(int productoId)
    {
        return _productoRepository.ObtenerActivoPorId(productoId);
    }

    public bool DarDeBaja(int productoId)
    {
        return _productoRepository.DarDeBaja(productoId);
    }

    private static ProductoInput CrearInputEdicion(ProductoDetalle producto)
    {
        return new ProductoInput
        {
            Nombre = producto.Nombre,
            DescripcionEspecifica = producto.DescripcionEspecifica,
            FechaVencimiento = producto.FechaVencimiento,
            Stock = producto.Stock,
            PrecioVenta = producto.PrecioVenta,
            CostoAdquisicion = producto.CostoAdquisicionActual,
            CategoriaId = producto.CategoriaId,
            MarcaId = producto.MarcaId
        };
    }
}

using Libreria.Application.Domain;
using Libreria.Application.Models;
using Libreria.Application.Ports.Secondary;
using Libreria.Application.Factories;


namespace Libreria.Application.Services;

public interface IProductoService
{
    ProductoFormulario ObtenerFormulario();

    Task<ResultadoOperacion> RegistrarAsync(
    ProductoInput input,
    ICrudRepository<Producto> repository);

    Task<ProductoListado> ObtenerListadoAsync(
    string? busqueda,
    int? categoriaId,
    int? marcaId,
    ICrudRepository<Producto> repository);

    Task<ProductoDetalle?> ObtenerDetalleAsync(
    Guid publicId,
    ICrudRepository<Producto> repository);

    Task<ProductoEdicion?> ObtenerEdicionAsync(
    Guid publicId,
    ICrudRepository<Producto> repository);

    Task<ResultadoOperacion> ActualizarAsync(
        Guid publicId,
        ProductoInput input,
        ICrudRepository<Producto> repository);

    Task<ProductoDetalle?> ObtenerParaBajaAsync(
    Guid publicId,
    ICrudRepository<Producto> repository);

    Task<bool> DarDeBajaAsync(
        Guid publicId,
        ICrudRepository<Producto> repository);
}

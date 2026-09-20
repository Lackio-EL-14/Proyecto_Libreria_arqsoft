using Libreria.Web.Pages.Productos.Models;
using Libreria.Web.Data.Repositories;
using Libreria.Web.Domain.Entities;


namespace Libreria.Web.Pages.Productos.Services;

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

using Libreria.Servicio.Productos.Domain.Entities;

namespace Libreria.Servicio.Productos.Application.Abstractions;

public interface IProductoRepository : ICrudRepository<Producto>
{
    Task<Producto?> ObtenerParaVentaAsync(Guid publicId);

    Task<bool> DescontarStockAsync(
        Guid publicId,
        int cantidad);

    Task<bool> RestaurarStockAsync(
        Guid publicId,
        int cantidad);

    Task ActualizarCategoriaEmbebidaAsync(
        Guid categoriaPublicId,
        string nuevoNombre);

    Task ActualizarMarcaEmbebidaAsync(
        Guid marcaPublicId,
        string nuevoNombre);
}
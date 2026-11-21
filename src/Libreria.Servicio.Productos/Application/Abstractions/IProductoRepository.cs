using Libreria.Servicio.Productos.Application.Models.Productos;
using Libreria.Servicio.Productos.Domain.Entities;

namespace Libreria.Servicio.Productos.Application.Abstractions;

public interface IProductoRepository : ICrudRepository<Producto>
{
    Task ActualizarCategoriaEmbebidaAsync(
        Guid categoriaPublicId,
        string nuevoNombre);

    Task ActualizarMarcaEmbebidaAsync(
        Guid marcaPublicId,
        string nuevoNombre);

    Task<IReadOnlyList<Producto>> ObtenerFiltradasAsync(
        string? busqueda,
        Guid? categoriaPublicId,
        Guid? marcaPublicId,
        bool? estado,
        OrdenProductos orden = OrdenProductos.Nombre,
        bool descendente = false);
}

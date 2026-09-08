using Libreria.Web.Pages.Productos.Models;
using System.Data;

namespace Libreria.Web.Pages.Productos.Repositories;

public interface IProductoRepository
{
    List<ProductoListItem> ObtenerProductos(
        string? busqueda,
        int? categoriaId,
        int? marcaId);

    ProductoDetalle? ObtenerProductoPorId(int productoId);

    List<CategoriaFiltroItem> ObtenerCategoriasActivas();

    List<MarcaFiltroItem> ObtenerMarcasActivas();

    List<CategoriaOption> ObtenerCategoriasParaFormulario();

    List<MarcaOption> ObtenerMarcasParaFormulario();

    bool ExisteCategoriaActiva(int categoriaId);

    bool ExisteMarcaActiva(int marcaId);

    bool ActualizarProducto(int productoId, ProductoInput input);

    bool DarDeBaja(int productoId);

    int CrearProducto(
    ProductoInput input,
    IDbConnection connection,
    IDbTransaction transaction);
}
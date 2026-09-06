using Libreria.Web.Pages.Productos.Models;
using System.Data;

namespace Libreria.Web.Pages.Productos.Repositories;

public interface IProductoRepository
{
    List<ProductoListItem> ObtenerProductos(
        string? busqueda,
        int? categoriaId,
        int? marcaId);

    List<CategoriaFiltroItem> ObtenerCategoriasActivas();

    List<MarcaFiltroItem> ObtenerMarcasActivas();

    List<CategoriaOption> ObtenerCategoriasParaFormulario();

    List<MarcaOption> ObtenerMarcasParaFormulario();

    bool ExisteCategoriaActiva(int categoriaId);

    bool ExisteMarcaActiva(int marcaId);

    int CrearProducto(
    ProductoInput input,
    IDbConnection connection,
    IDbTransaction transaction);
}
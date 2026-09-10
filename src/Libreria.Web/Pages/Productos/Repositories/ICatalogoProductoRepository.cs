using Libreria.Web.Pages.Productos.Models;

namespace Libreria.Web.Pages.Productos.Repositories;

public interface ICatalogoProductoRepository
{
    IReadOnlyList<CategoriaOption> ObtenerCategoriasActivas();
    IReadOnlyList<MarcaOption> ObtenerMarcasActivas();
    bool ExisteCategoriaActiva(int categoriaId);
    bool ExisteMarcaActiva(int marcaId);
}

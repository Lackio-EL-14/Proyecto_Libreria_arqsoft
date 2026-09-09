using Libreria.Web.Pages.Productos.Models;

namespace Libreria.Web.Pages.Productos.Repositories;

public interface IConsultaCatalogoProductoRepository
{
    IReadOnlyList<CategoriaOption> ObtenerCategoriasActivas();
    IReadOnlyList<MarcaOption> ObtenerMarcasActivas();
}

using Libreria.Web.Pages.Categorias.Models;

namespace Libreria.Web.Pages.Categorias.Repositories;

public interface IListadoCategoriasRepository
{
    Task<IReadOnlyList<Categoria>> ObtenerActivasAsync(string? busqueda);
}

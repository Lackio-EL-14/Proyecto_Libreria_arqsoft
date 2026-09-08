using Libreria.Web.Pages.Categorias.Models;

namespace Libreria.Web.Pages.Categorias.Repositories;

public interface IReactivacionCategoriaRepository
{
    Task<Categoria?> ObtenerInactivaPorIdAsync(int categoriaId);
    Task<bool> ReactivarAsync(int categoriaId);
}

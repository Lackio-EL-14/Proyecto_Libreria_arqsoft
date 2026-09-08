using Libreria.Web.Pages.Categorias.Models;

namespace Libreria.Web.Pages.Categorias.Repositories;

public interface IEdicionCategoriaRepository
{
    Task<Categoria?> ObtenerActivaPorIdAsync(int categoriaId);
    Task<bool> ActualizarAsync(Categoria categoria);
}

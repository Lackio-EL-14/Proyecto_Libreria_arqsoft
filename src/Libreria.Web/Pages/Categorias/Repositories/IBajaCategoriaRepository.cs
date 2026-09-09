using Libreria.Web.Pages.Categorias.Models;

namespace Libreria.Web.Pages.Categorias.Repositories;

public interface IBajaCategoriaRepository
{
    Task<Categoria?> ObtenerActivaPorIdAsync(int categoriaId);
    Task<bool> TieneProductosActivosAsync(int categoriaId);
    Task<bool> DarDeBajaAsync(int categoriaId);
}

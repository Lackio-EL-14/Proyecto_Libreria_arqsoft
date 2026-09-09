using Libreria.Web.Pages.Categorias.Models;

namespace Libreria.Web.Pages.Categorias.Repositories;

public interface ICategoriaRepository
{
    Task<IReadOnlyList<Categoria>> ObtenerActivasAsync(string? busqueda);
    Task CrearAsync(Categoria categoria);
    Task<Categoria?> ObtenerActivaPorIdAsync(int categoriaId);
    Task<Categoria?> ObtenerInactivaPorIdAsync(int categoriaId);
    Task<bool> ActualizarAsync(Categoria categoria);
    Task<bool> TieneProductosActivosAsync(int categoriaId);
    Task<bool> DarDeBajaAsync(int categoriaId);
    Task<bool> ReactivarAsync(int categoriaId);
    Task<bool> ExisteCodigoAsync(string codigo, int? excluirId);
    Task<bool> ExisteNombreAsync(string nombre, int? excluirId);
}

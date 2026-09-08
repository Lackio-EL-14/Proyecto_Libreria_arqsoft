namespace Libreria.Web.Pages.Categorias.Repositories;

public interface IValidadorCategoriaRepository
{
    Task<bool> ExisteCodigoAsync(string codigo, int? excluirId);
    Task<bool> ExisteNombreAsync(string nombre, int? excluirId);
}

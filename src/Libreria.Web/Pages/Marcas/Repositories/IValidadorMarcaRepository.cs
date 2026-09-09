namespace Libreria.Web.Pages.Marcas.Repositories;

public interface IValidadorMarcaRepository
{
    bool ExisteNombre(string nombre, int? excluirId);
}

using Libreria.Web.Pages.Categorias.Models;

namespace Libreria.Web.Pages.Categorias.Repositories;

public interface IRegistroCategoriaRepository
{
    Task CrearAsync(Categoria categoria);
}

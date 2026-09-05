using System.Threading.Tasks;
using Libreria.Web.Pages.Categorias.Models;

namespace Libreria.Web.Pages.Categorias.Repositories
{
    public interface ICategoriaRepository
    {
        Task<bool> ExisteNombreAsync(string nombre);
        
        Task CrearAsync(Categoria categoria);
    }
}

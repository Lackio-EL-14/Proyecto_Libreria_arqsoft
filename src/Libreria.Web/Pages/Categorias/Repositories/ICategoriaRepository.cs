using System.Collections.Generic;
using System.Threading.Tasks;
using Libreria.Web.Pages.Categorias.Models;

namespace Libreria.Web.Pages.Categorias.Repositories
{
    public interface ICategoriaRepository
    {
        Task<bool> ExisteNombreAsync(string nombre, int? excluirId = null);
        
        Task CrearAsync(Categoria categoria);
        Task<IEnumerable<Categoria>> ObtenerTodasAsync(string? busqueda = null);
        
        Task<Categoria?> ObtenerPorIdAsync(int id);
        Task ActualizarAsync(Categoria categoria);

        Task<bool> TieneProductosActivosAsync(int categoriaId);
        Task DarDeBajaAsync(int id);
        Task ReactivarAsync(int id);
    }
}

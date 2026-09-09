using Libreria.Web.Pages.Marcas.Models;

namespace Libreria.Web.Pages.Marcas.Repositories;

public interface IEdicionMarcaRepository
{
    Marca? ObtenerActivaPorId(int marcaId);
    bool Actualizar(Marca marca);
}

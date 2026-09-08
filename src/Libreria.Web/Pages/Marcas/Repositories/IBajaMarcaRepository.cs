using Libreria.Web.Pages.Marcas.Models;

namespace Libreria.Web.Pages.Marcas.Repositories;

public interface IBajaMarcaRepository
{
    MarcaBajaView? ObtenerActivaParaBaja(int marcaId);
    bool DarDeBaja(int marcaId);
}

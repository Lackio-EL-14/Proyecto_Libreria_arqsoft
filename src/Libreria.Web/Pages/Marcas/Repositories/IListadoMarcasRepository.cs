using Libreria.Web.Pages.Marcas.Models;

namespace Libreria.Web.Pages.Marcas.Repositories;

public interface IListadoMarcasRepository
{
    IReadOnlyList<MarcaListItem> ObtenerActivas(string? nombre);
}

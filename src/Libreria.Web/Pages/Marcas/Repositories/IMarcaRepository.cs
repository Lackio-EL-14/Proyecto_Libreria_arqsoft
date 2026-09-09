using Libreria.Web.Pages.Marcas.Models;

namespace Libreria.Web.Pages.Marcas.Repositories;

public interface IMarcaRepository
{
    IReadOnlyList<MarcaListItem> ObtenerActivas(string? nombre);
    void Crear(Marca marca);
    Marca? ObtenerActivaPorId(int marcaId);
    bool Actualizar(Marca marca);
    MarcaBajaView? ObtenerActivaParaBaja(int marcaId);
    bool DarDeBaja(int marcaId);
    bool ExisteNombre(string nombre, int? excluirId);
}

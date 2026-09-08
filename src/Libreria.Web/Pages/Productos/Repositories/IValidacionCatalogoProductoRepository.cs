namespace Libreria.Web.Pages.Productos.Repositories;

public interface IValidacionCatalogoProductoRepository
{
    bool ExisteCategoriaActiva(int categoriaId);
    bool ExisteMarcaActiva(int marcaId);
}

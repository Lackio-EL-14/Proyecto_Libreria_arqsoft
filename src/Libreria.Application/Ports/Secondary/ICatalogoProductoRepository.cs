using Libreria.Application.Domain;
using Libreria.Application.Models;
using Libreria.Application.Ports.Secondary;
using Libreria.Application.Factories;

namespace Libreria.Application.Ports.Secondary;

public interface ICatalogoProductoRepository
{
    IReadOnlyList<CategoriaOption> ObtenerCategoriasActivas();
    IReadOnlyList<MarcaOption> ObtenerMarcasActivas();
    bool ExisteCategoriaActiva(int categoriaId);
    bool ExisteMarcaActiva(int marcaId);
}

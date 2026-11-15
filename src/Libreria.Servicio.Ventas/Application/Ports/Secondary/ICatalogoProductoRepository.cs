using Libreria.Servicio.Ventas.Domain;
using Libreria.Servicio.Ventas.Application.Models;
using Libreria.Servicio.Ventas.Application.Ports.Secondary;
using Libreria.Servicio.Ventas.Application.Factories;

namespace Libreria.Servicio.Ventas.Application.Ports.Secondary;

public interface ICatalogoProductoRepository
{
    IReadOnlyList<CategoriaOption> ObtenerCategoriasActivas();
    IReadOnlyList<MarcaOption> ObtenerMarcasActivas();
    bool ExisteCategoriaActiva(int categoriaId);
    bool ExisteMarcaActiva(int marcaId);
}

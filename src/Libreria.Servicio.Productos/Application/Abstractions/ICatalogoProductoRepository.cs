using Libreria.Servicio.Productos.Domain.Entities;

namespace Libreria.Servicio.Productos.Application.Abstractions;

public interface ICatalogoProductoRepository
{
    Task<IReadOnlyList<Categoria>> ObtenerCategoriasActivasAsync();

    Task<IReadOnlyList<Marca>> ObtenerMarcasActivasAsync();

    Task<bool> ExisteCategoriaActivaAsync(Guid publicId);

    Task<bool> ExisteMarcaActivaAsync(Guid publicId);
}
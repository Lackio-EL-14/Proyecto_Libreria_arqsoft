using Libreria.Domain;

namespace Libreria.Application.Ports.Secondary;

public interface IRolRepository
{
    Task<IReadOnlyList<Rol>> ObtenerActivosAsync();
}
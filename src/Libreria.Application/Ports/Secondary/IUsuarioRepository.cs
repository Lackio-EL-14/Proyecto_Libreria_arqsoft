using Libreria.Application.Domain;

namespace Libreria.Application.Ports.Secondary;

public interface IUsuarioRepository
{
    Task<Usuario?> ObtenerActivoPorNombreUsuarioAsync(string nombreUsuario);

    // US-40: incluye usuarios inactivos, la trazabilidad no se pierde.
    Task<string?> ObtenerNombreCompletoPorIdAsync(int usuarioId);
}

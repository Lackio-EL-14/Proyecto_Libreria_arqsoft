using Libreria.Application.Domain;

namespace Libreria.Application.Ports.Secondary;

public interface IUsuarioRepository
{
    Task<Usuario?> ObtenerActivoPorNombreUsuarioAsync(string nombreUsuario);
}

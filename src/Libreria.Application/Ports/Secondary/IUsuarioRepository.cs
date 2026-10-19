using Libreria.Domain;
using System.Data;

namespace Libreria.Application.Ports.Secondary;

public interface IUsuarioRepository
{
    Task<Usuario?> ObtenerActivoPorNombreUsuarioAsync(string nombreUsuario);

    Task<string?> ObtenerNombreCompletoPorIdAsync(int usuarioId);

    Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario);

    Task<bool> ExisteCorreoAsync(string correo);

    Task<int> InsertarAsync(Usuario usuario);

    Task<int> InsertarAsync(
    Usuario usuario,
    IDbConnection connection,
    IDbTransaction transaction);
}
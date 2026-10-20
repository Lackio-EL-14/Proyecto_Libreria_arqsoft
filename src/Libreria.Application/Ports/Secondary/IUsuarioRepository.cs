using Libreria.Domain;
using Libreria.Application.Models;
using System.Data;

namespace Libreria.Application.Ports.Secondary;

public interface IUsuarioRepository
{
    Task<string?> ObtenerHashActivoAsync(int usuarioId);
    Task<bool> ActualizarContrasenaAsync(int usuarioId, string hashAnterior, string nuevoHash);

    Task<IReadOnlyList<UsuarioListItemModel>> ObtenerListadoAsync(string? estado = null);

    Task<Usuario?> ObtenerActivoPorNombreUsuarioAsync(string nombreUsuario);

    Task<string?> ObtenerNombreCompletoPorIdAsync(int usuarioId);

    Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario);

    Task<bool> ExisteCorreoAsync(string correo);

    Task<int> InsertarAsync(Usuario usuario);

    Task<int> InsertarAsync(
        Usuario usuario,
        IDbConnection connection,
        IDbTransaction transaction);

    Task<Usuario?> ObtenerPorTokenActivacionHashAsync(string tokenHash);

    Task<bool> ActivarAsync(
        int usuarioId,
        string passwordHash,
        DateTime fechaActivacion);
}

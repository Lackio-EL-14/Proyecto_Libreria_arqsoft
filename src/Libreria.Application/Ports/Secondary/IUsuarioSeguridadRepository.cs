using System.Data;
using Libreria.Application.Models;

namespace Libreria.Application.Ports.Secondary;

public interface IUsuarioSeguridadRepository
{
    Task<bool> TieneAccesoActivoAsync(int usuarioId);
    Task<IReadOnlyList<UsuarioBajaDatos>> BloquearParaBajaAsync(
        IDbConnection connection, IDbTransaction transaction);
    Task<bool> DesactivarAsync(int usuarioId, int usuarioModificacionId,
        IDbConnection connection, IDbTransaction transaction);
}

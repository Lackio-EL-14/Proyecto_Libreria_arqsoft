using System.Data;
using Libreria.Application.Models;
using Libreria.Application.Ports.Primary;
using Libreria.Application.Ports.Secondary;
using Libreria.Domain;

namespace Libreria.Application.Services;

public class UsuarioSeguridadService : IUsuarioSeguridadService
{
    private readonly IUsuarioSeguridadRepository _repository;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IUsuarioActual _usuarioActual;

    public UsuarioSeguridadService(IUsuarioSeguridadRepository repository,
        IDbConnectionFactory connectionFactory, IUsuarioActual usuarioActual)
    {
        _repository = repository;
        _connectionFactory = connectionFactory;
        _usuarioActual = usuarioActual;
    }

    public Task<bool> SesionActivaAsync(int usuarioId) => usuarioId > 0
        ? _repository.TieneAccesoActivoAsync(usuarioId)
        : Task.FromResult(false);

    public async Task<ResultadoOperacion> DarDeBajaAsync(Guid publicId)
    {
        var actorId = _usuarioActual.UsuarioId;
        if (actorId is not > 0)
            return Error("Debe iniciar sesión como administrador.");
        if (publicId == Guid.Empty)
            return ResultadoOperacion.NoExiste();

        await using var connection = _connectionFactory.CreateConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            // La instantánea queda bloqueada hasta confirmar o liberar la transacción.
            var usuarios = await _repository.BloquearParaBajaAsync(connection, transaction);
            var actor = usuarios.SingleOrDefault(u => u.UsuarioId == actorId);
            if (actor is null || !EsAdministradorActivo(actor))
                return Error("Solo un administrador activo puede dar de baja usuarios.");

            var usuario = usuarios.SingleOrDefault(u => u.PublicId == publicId);
            if (usuario is null)
                return ResultadoOperacion.NoExiste();
            if (usuario.Estado == EstadosUsuario.Inactivo)
                return Error("El usuario ya se encuentra inactivo.");
            if (EsAdministradorActivo(usuario) && usuarios.Count(EsAdministradorActivo) <= 1)
                return Error("No se puede dar de baja al último administrador activo.");
            if (usuario.UsuarioId == actorId)
                return Error("No puede darse de baja a sí mismo.");

            if (!await _repository.DesactivarAsync(usuario.UsuarioId, actorId.Value, connection, transaction))
                throw new InvalidOperationException("No se pudo actualizar el estado del usuario.");

            await transaction.CommitAsync();
            return ResultadoOperacion.Correcto();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static bool EsAdministradorActivo(UsuarioBajaDatos usuario) =>
        usuario.Estado == EstadosUsuario.Activo && usuario.RolActivo &&
        usuario.Rol == RolesSistema.Administrador;

    private static ResultadoOperacion Error(string mensaje) =>
        ResultadoOperacion.Invalido(new Dictionary<string, string> { [string.Empty] = mensaje });
}

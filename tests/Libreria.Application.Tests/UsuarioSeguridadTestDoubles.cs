using System.Data;
using System.Data.Common;
using Libreria.Application.Models;
using Libreria.Application.Ports.Primary;
using Libreria.Application.Ports.Secondary;

namespace Libreria.Application.Tests;

internal sealed class UsuarioSeguridadServiceFake : IUsuarioSeguridadService
{
    public bool Activa { get; set; } = true;
    public int Consultas { get; private set; }
    public int Bajas { get; private set; }
    public ResultadoOperacion Resultado { get; set; } = ResultadoOperacion.Correcto();
    public Task<bool> SesionActivaAsync(int usuarioId)
    {
        Consultas++;
        return Task.FromResult(usuarioId > 0 && Activa);
    }
    public Task<ResultadoOperacion> DarDeBajaAsync(Guid publicId)
    {
        Bajas++;
        return Task.FromResult(Resultado);
    }
}

internal sealed class SeguridadRepositoryFake(params UsuarioBajaDatos[] usuarios) : IUsuarioSeguridadRepository
{
    public bool Activo { get; set; } = true;
    public bool FallarActualizacion { get; set; }
    public int Consultas { get; private set; }
    public int? BajaId { get; private set; }
    public int? ActorId { get; private set; }
    public IDbTransaction? TransaccionLectura { get; private set; }
    public IDbTransaction? TransaccionEscritura { get; private set; }

    public Task<bool> TieneAccesoActivoAsync(int usuarioId)
    {
        Consultas++;
        return Task.FromResult(Activo);
    }
    public Task<IReadOnlyList<UsuarioBajaDatos>> BloquearParaBajaAsync(
        IDbConnection connection, IDbTransaction transaction)
    {
        TransaccionLectura = transaction;
        return Task.FromResult<IReadOnlyList<UsuarioBajaDatos>>(usuarios);
    }
    public Task<bool> DesactivarAsync(int usuarioId, int usuarioModificacionId,
        IDbConnection connection, IDbTransaction transaction)
    {
        if (FallarActualizacion) throw new InvalidOperationException("Fallo simulado de SQL.");
        BajaId = usuarioId;
        ActorId = usuarioModificacionId;
        TransaccionEscritura = transaction;
        return Task.FromResult(true);
    }
}

internal sealed class UsuarioActualFake(int? id) : IUsuarioActual
{
    public int? UsuarioId => id;
}

internal sealed class ConexionBajaFake : DbConnection, IDbConnectionFactory
{
    private ConnectionState _state = ConnectionState.Closed;
    [System.Diagnostics.CodeAnalysis.AllowNull]
    public override string ConnectionString { get; set; } = string.Empty;
    public override string Database => "Pruebas";
    public override string DataSource => "Pruebas";
    public override string ServerVersion => "1";
    public override ConnectionState State => _state;
    public TransaccionBajaFake? Transaccion { get; private set; }
    public DbConnection CreateConnection() => this;
    public override void Open() => _state = ConnectionState.Open;
    public override void Close() => _state = ConnectionState.Closed;
    public override void ChangeDatabase(string databaseName) => throw new NotSupportedException();
    protected override DbCommand CreateDbCommand() => throw new NotSupportedException();
    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        => Transaccion = new TransaccionBajaFake(this, isolationLevel);
}

internal sealed class TransaccionBajaFake(DbConnection connection, IsolationLevel isolationLevel) : DbTransaction
{
    public bool Confirmada { get; private set; }
    public bool Revertida { get; private set; }
    public override IsolationLevel IsolationLevel => isolationLevel;
    protected override DbConnection DbConnection => connection;
    public override void Commit() => Confirmada = true;
    public override void Rollback() => Revertida = true;
}

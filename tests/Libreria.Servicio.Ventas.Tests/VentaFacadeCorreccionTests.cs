using System.Data;
using System.Data.Common;
using Libreria.Servicio.Ventas.Application.Comun;
using Libreria.Servicio.Ventas.Application.Facades;
using Libreria.Servicio.Ventas.Application.Models;
using Libreria.Servicio.Ventas.Application.Ports.Secondary;
using Libreria.Servicio.Ventas.Domain;

namespace Libreria.Servicio.Ventas.Tests;

public class VentaFacadeCorreccionTests
{
    [Fact]
    public async Task ObservacionMayorA250_DevuelveValidacion()
    {
        var facade = CrearFacade(new VentaRepositoryFake());

        var resultado = await facade.CorregirDatosAsync(
            Guid.NewGuid(),
            new CorregirVentaInput
            {
                CiNitCliente = "1234567",
                Observacion = new string('x', 251)
            });

        Assert.Equal(TipoError.Validacion, resultado.Tipo);
        Assert.Contains("250", resultado.Errores["Observacion"]);
    }

    [Fact]
    public async Task VentaInexistente_DevuelveNoEncontrado()
    {
        var facade = CrearFacade(new VentaRepositoryFake());

        var resultado = await facade.CorregirDatosAsync(
            Guid.NewGuid(),
            new CorregirVentaInput { CiNitCliente = "1234567" });

        Assert.Equal(TipoError.NoEncontrado, resultado.Tipo);
    }

    [Fact]
    public async Task VentaAnulada_DevuelveConflictoSinConsultarClientes()
    {
        var clientes = new ClientesPortFake();
        var facade = CrearFacade(
            new VentaRepositoryFake { Venta = CrearVenta("Anulada") },
            clientes);

        var resultado = await facade.CorregirDatosAsync(
            Guid.NewGuid(),
            new CorregirVentaInput { CiNitCliente = "1234567" });

        Assert.Equal(TipoError.Conflicto, resultado.Tipo);
        Assert.Equal(0, clientes.Consultas);
    }

    [Fact]
    public async Task ClienteActivo_ActualizaCopiaObservacionYAuditoriaEnTransaccion()
    {
        var publicId = Guid.NewGuid();
        var venta = CrearVenta("Activa");
        venta.PublicId = publicId;
        var repositorio = new VentaRepositoryFake { Venta = venta };
        var cliente = new ClienteVentaSnapshot(Guid.NewGuid(), "7654321", "Cliente corregido");
        var facade = CrearFacade(
            repositorio,
            new ClientesPortFake(Result<ClienteVentaSnapshot>.Exito(cliente)));

        var resultado = await facade.CorregirDatosAsync(
            publicId,
            new CorregirVentaInput
            {
                CiNitCliente = "7654321",
                Observacion = "  Entrega reprogramada  "
            });

        Assert.True(resultado.EsExito);
        Assert.Equal(cliente, repositorio.ClienteActualizado);
        Assert.Equal("Entrega reprogramada", repositorio.ObservacionActualizada);
        Assert.Equal(UsuarioActualFake.Id, repositorio.UsuarioModificacionId);
        Assert.True(repositorio.TransaccionUsada);
    }

    private static VentaFacade CrearFacade(
        VentaRepositoryFake repositorio,
        ClientesPortFake? clientes = null) =>
        new(
            repositorio,
            null!,
            clientes ?? new ClientesPortFake(),
            new ConnectionFactoryFake(),
            new UsuarioActualFake(),
            null!,
            null!);

    private static Venta CrearVenta(string estado) =>
        new()
        {
            VentaId = 7,
            PublicId = Guid.NewGuid(),
            ClientePublicId = Guid.NewGuid(),
            ClienteCiNit = "1234567",
            ClienteRazonSocial = "Cliente original",
            Estado = estado,
            UsuarioCreacionId = Guid.NewGuid()
        };

    private sealed class ClientesPortFake : IClientesPort
    {
        private readonly Result<ClienteVentaSnapshot> _resultado;

        public ClientesPortFake(Result<ClienteVentaSnapshot>? resultado = null)
        {
            _resultado = resultado ?? Result<ClienteVentaSnapshot>.Fallo(
                Result.NoEncontrado("El cliente no existe o está inactivo."));
        }

        public int Consultas { get; private set; }

        public Task<Result<ClienteVentaSnapshot>> ObtenerPorCiNitAsync(
            string ciNit,
            CancellationToken cancellationToken = default)
        {
            Consultas++;
            return Task.FromResult(_resultado);
        }
    }

    private sealed class UsuarioActualFake : IUsuarioActual
    {
        public static readonly Guid Id = Guid.Parse("a0000000-0000-0000-0000-000000000001");
        public Guid? UsuarioId => Id;
        public string? NombreUsuario => "admin";
        public string? Iniciales => "A.S.";
        public bool EsAdministrador => true;
    }

    private sealed class ConnectionFactoryFake : IDbConnectionFactory
    {
        public DbConnection CreateConnection() => new ConnectionFake();
    }

    private sealed class ConnectionFake : DbConnection
    {
        private ConnectionState _state = ConnectionState.Closed;
        [System.Diagnostics.CodeAnalysis.AllowNull]
        public override string ConnectionString { get; set; } = string.Empty;
        public override string Database => "VentasTest";
        public override string DataSource => "Fake";
        public override string ServerVersion => "1";
        public override ConnectionState State => _state;
        public override void ChangeDatabase(string databaseName) { }
        public override void Close() => _state = ConnectionState.Closed;
        public override void Open() => _state = ConnectionState.Open;
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) =>
            new TransactionFake(this, isolationLevel);
        protected override DbCommand CreateDbCommand() => throw new NotSupportedException();
    }

    private sealed class TransactionFake(DbConnection connection, IsolationLevel isolationLevel) : DbTransaction
    {
        public override IsolationLevel IsolationLevel => isolationLevel;
        protected override DbConnection DbConnection => connection;
        public override void Commit() { }
        public override void Rollback() { }
    }

    private sealed class VentaRepositoryFake : IVentaRepository
    {
        public Venta? Venta { get; init; }
        public ClienteVentaSnapshot? ClienteActualizado { get; private set; }
        public string? ObservacionActualizada { get; private set; }
        public Guid? UsuarioModificacionId { get; private set; }
        public bool TransaccionUsada { get; private set; }

        public Task<Venta?> ObtenerPorPublicIdAsync(Guid publicId) => Task.FromResult(Venta);

        public Task<Venta?> ObtenerPorPublicIdAsync(
            Guid publicId,
            IDbConnection connection,
            IDbTransaction transaction)
        {
            TransaccionUsada = true;
            return Task.FromResult(Venta);
        }

        public Task<bool> ActualizarDatosAsync(
            int ventaId,
            ClienteVentaSnapshot cliente,
            string? observacion,
            Guid usuarioModificacionId,
            IDbConnection connection,
            IDbTransaction transaction)
        {
            ClienteActualizado = cliente;
            ObservacionActualizada = observacion;
            UsuarioModificacionId = usuarioModificacionId;
            return Task.FromResult(true);
        }

        public Task<int> InsertarAsync(Venta venta, IDbConnection connection, IDbTransaction transaction) => throw new NotImplementedException();
        public Task InsertarDetalleAsync(DetalleVenta detalle, IDbConnection connection, IDbTransaction transaction) => throw new NotImplementedException();
        public Task<bool> ActualizarEstadoAsync(int ventaId, string estado, Guid? usuarioAnulacionId, string? motivoAnulacion, IDbConnection connection, IDbTransaction transaction) => throw new NotImplementedException();
        public Task<IReadOnlyList<Venta>> ObtenerActivasAsync() => throw new NotImplementedException();
        public Task<IReadOnlyList<Venta>> ObtenerTodasAsync() => throw new NotImplementedException();
        public Task<IReadOnlyList<DetalleVenta>> ObtenerDetallePorVentaIdAsync(int ventaId) => throw new NotImplementedException();
        public Task<IReadOnlyList<DetalleVenta>> ObtenerDetallePorVentaIdAsync(int ventaId, IDbConnection connection, IDbTransaction transaction) => throw new NotImplementedException();
        public Task<IReadOnlyList<VentaListItemModel>> ObtenerListadoAsync(FiltroVentasInput filtro, bool esAdministrador) => throw new NotImplementedException();
        public Task<VentaDetalleCompletoModel?> ObtenerDetalleCompletoAsync(Guid publicId, bool esAdministrador) => throw new NotImplementedException();
        public Task<ComprobanteVenta?> ObtenerComprobanteAsync(Guid publicId) => throw new NotImplementedException();
    }
}

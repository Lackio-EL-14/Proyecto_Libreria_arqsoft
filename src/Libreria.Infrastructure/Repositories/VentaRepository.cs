using Libreria.Application.Domain;
using Libreria.Application.Ports.Secondary;
using System.Data;
using System.Data.Common;

namespace Libreria.Infrastructure;

public class VentaRepository : IVentaRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public VentaRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> InsertarAsync(
        Venta venta,
        IDbConnection connection,
        IDbTransaction transaction)
    {
        var (dbConnection, dbTransaction) =
            ObtenerConexionYTransaccion(connection, transaction);

        if (venta.PublicId == Guid.Empty)
        {
            venta.PublicId = Guid.NewGuid();
        }

        await using var command = dbConnection.CreateCommand();
        command.Transaction = dbTransaction;

        command.CommandText = @"
            INSERT INTO Venta
                (PublicId, ClienteId, Estado,
                 UsuarioCreacionId, UsuarioAnulacionId)
            OUTPUT INSERTED.VentaId
            VALUES
                (@PublicId, @ClienteId, @Estado,
                 @UsuarioCreacionId, @UsuarioAnulacionId)";

        AgregarParametro(command, "@PublicId", venta.PublicId);
        AgregarParametro(command, "@ClienteId", venta.ClienteId);
        AgregarParametro(command, "@Estado", venta.Estado);
        AgregarParametro(
            command,
            "@UsuarioCreacionId",
            venta.UsuarioCreacionId);
        AgregarParametro(
            command,
            "@UsuarioAnulacionId",
            venta.UsuarioAnulacionId ?? (object)DBNull.Value);

        var resultado = await command.ExecuteScalarAsync();
        venta.VentaId = Convert.ToInt32(resultado);

        return venta.VentaId;
    }

    public async Task InsertarDetalleAsync(
        DetalleVenta detalle,
        IDbConnection connection,
        IDbTransaction transaction)
    {
        var (dbConnection, dbTransaction) =
            ObtenerConexionYTransaccion(connection, transaction);

        await using var command = dbConnection.CreateCommand();
        command.Transaction = dbTransaction;

        command.CommandText = @"
            INSERT INTO DetalleVenta
                (VentaId, ProductoId, Cantidad,
                 PrecioUnitarioVenta, CostoAdquisicionUnitario,
                 Importe, Ganancia)
            VALUES
                (@VentaId, @ProductoId, @Cantidad,
                 @PrecioUnitarioVenta, @CostoAdquisicionUnitario,
                 @Importe, @Ganancia)";

        AgregarParametro(command, "@VentaId", detalle.VentaId);
        AgregarParametro(command, "@ProductoId", detalle.ProductoId);
        AgregarParametro(command, "@Cantidad", detalle.Cantidad);
        AgregarParametro(
            command,
            "@PrecioUnitarioVenta",
            detalle.PrecioUnitarioVenta);
        AgregarParametro(
            command,
            "@CostoAdquisicionUnitario",
            detalle.CostoAdquisicionUnitario);
        AgregarParametro(command, "@Importe", detalle.Importe);
        AgregarParametro(command, "@Ganancia", detalle.Ganancia);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<bool> ActualizarEstadoAsync(
        int ventaId,
        string estado,
        int? usuarioAnulacionId,
        IDbConnection connection,
        IDbTransaction transaction)
    {
        var (dbConnection, dbTransaction) =
            ObtenerConexionYTransaccion(connection, transaction);

        await using var command = dbConnection.CreateCommand();
        command.Transaction = dbTransaction;

        command.CommandText = @"
            UPDATE Venta
            SET Estado = @Estado,
                UsuarioAnulacionId = @UsuarioAnulacionId
            WHERE VentaId = @VentaId";

        AgregarParametro(command, "@VentaId", ventaId);
        AgregarParametro(command, "@Estado", estado);
        AgregarParametro(
            command,
            "@UsuarioAnulacionId",
            usuarioAnulacionId ?? (object)DBNull.Value);

        return await command.ExecuteNonQueryAsync() == 1;
    }

    public async Task<IReadOnlyList<Venta>> ObtenerActivasAsync()
    {
        var ventas = new List<Venta>();

        await using var connection = await CrearConexionAbiertaAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT VentaId, PublicId, ClienteId, Estado,
                   UsuarioCreacionId, UsuarioAnulacionId
            FROM Venta
            WHERE Estado = N'Activa'
            ORDER BY VentaId DESC";

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            ventas.Add(MapearVenta(reader));
        }

        return ventas;
    }

    public async Task<Venta?> ObtenerPorPublicIdAsync(Guid publicId)
    {
        await using var connection = await CrearConexionAbiertaAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT VentaId, PublicId, ClienteId, Estado,
                   UsuarioCreacionId, UsuarioAnulacionId
            FROM Venta
            WHERE PublicId = @PublicId";

        AgregarParametro(command, "@PublicId", publicId);

        await using var reader = await command.ExecuteReaderAsync();

        return await reader.ReadAsync()
            ? MapearVenta(reader)
            : null;
    }

    public async Task<IReadOnlyList<DetalleVenta>> ObtenerDetallePorVentaIdAsync(
        int ventaId)
    {
        var detalles = new List<DetalleVenta>();

        await using var connection = await CrearConexionAbiertaAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT DetalleVentaId, VentaId, ProductoId, Cantidad,
                   PrecioUnitarioVenta, CostoAdquisicionUnitario,
                   Importe, Ganancia
            FROM DetalleVenta
            WHERE VentaId = @VentaId
            ORDER BY DetalleVentaId";

        AgregarParametro(command, "@VentaId", ventaId);

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            detalles.Add(MapearDetalle(reader));
        }

        return detalles;
    }

    private async Task<DbConnection> CrearConexionAbiertaAsync()
    {
        var connection = _connectionFactory.CreateConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        return connection;
    }

    private static Venta MapearVenta(DbDataReader reader)
    {
        return new Venta
        {
            VentaId = reader.GetInt32(reader.GetOrdinal("VentaId")),
            PublicId = reader.GetGuid(reader.GetOrdinal("PublicId")),
            ClienteId = reader.GetInt32(reader.GetOrdinal("ClienteId")),
            Estado = reader.GetString(reader.GetOrdinal("Estado")),
            UsuarioCreacionId =
                reader.GetInt32(reader.GetOrdinal("UsuarioCreacionId")),
            UsuarioAnulacionId =
                reader.IsDBNull(reader.GetOrdinal("UsuarioAnulacionId"))
                    ? null
                    : reader.GetInt32(
                        reader.GetOrdinal("UsuarioAnulacionId"))
        };
    }

    private static DetalleVenta MapearDetalle(DbDataReader reader)
    {
        return new DetalleVenta
        {
            DetalleVentaId =
                reader.GetInt32(reader.GetOrdinal("DetalleVentaId")),
            VentaId = reader.GetInt32(reader.GetOrdinal("VentaId")),
            ProductoId = reader.GetInt32(reader.GetOrdinal("ProductoId")),
            Cantidad = reader.GetInt32(reader.GetOrdinal("Cantidad")),
            PrecioUnitarioVenta =
                reader.GetDecimal(reader.GetOrdinal("PrecioUnitarioVenta")),
            CostoAdquisicionUnitario =
                reader.GetDecimal(
                    reader.GetOrdinal("CostoAdquisicionUnitario")),
            Importe = reader.GetDecimal(reader.GetOrdinal("Importe")),
            Ganancia = reader.GetDecimal(reader.GetOrdinal("Ganancia"))
        };
    }

    private static (DbConnection Connection, DbTransaction Transaction)
        ObtenerConexionYTransaccion(
            IDbConnection connection,
            IDbTransaction transaction)
    {
        if (connection is not DbConnection dbConnection)
        {
            throw new ArgumentException(
                "La conexión debe ser compatible con DbConnection.",
                nameof(connection));
        }

        if (transaction is not DbTransaction dbTransaction)
        {
            throw new ArgumentException(
                "La transacción debe ser compatible con DbTransaction.",
                nameof(transaction));
        }

        return (dbConnection, dbTransaction);
    }

    private static void AgregarParametro(
        DbCommand command,
        string nombre,
        object valor)
    {
        var parametro = command.CreateParameter();
        parametro.ParameterName = nombre;
        parametro.Value = valor;
        command.Parameters.Add(parametro);
    }
}
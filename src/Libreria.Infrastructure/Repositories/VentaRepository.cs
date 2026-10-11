using Libreria.Application.Domain;
using Libreria.Application.Models;
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
                 FechaCreacion, UsuarioCreacionId, UsuarioAnulacionId)
            OUTPUT INSERTED.VentaId
            VALUES
                (@PublicId, @ClienteId, @Estado,
                 SYSDATETIME(), @UsuarioCreacionId, @UsuarioAnulacionId)";

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
                UsuarioAnulacionId = @UsuarioAnulacionId,
                FechaAnulacion = CASE WHEN @Estado = N'Anulada' THEN SYSDATETIME() ELSE FechaAnulacion END
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
                   FechaCreacion, FechaAnulacion,
                   UsuarioCreacionId, UsuarioAnulacionId
            FROM Venta
            WHERE Estado = N'Activa'
            ORDER BY FechaCreacion DESC, VentaId DESC";

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            ventas.Add(MapearVenta(reader));
        }

        return ventas;
    }

    public async Task<IReadOnlyList<Venta>> ObtenerTodasAsync()
    {
        var ventas = new List<Venta>();

        await using var connection = await CrearConexionAbiertaAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT VentaId, PublicId, ClienteId, Estado,
                   FechaCreacion, FechaAnulacion,
                   UsuarioCreacionId, UsuarioAnulacionId
            FROM Venta
            ORDER BY FechaCreacion DESC, VentaId DESC";

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
                   FechaCreacion, FechaAnulacion,
                   UsuarioCreacionId, UsuarioAnulacionId
            FROM Venta
            WHERE PublicId = @PublicId";

        AgregarParametro(command, "@PublicId", publicId);

        await using var reader = await command.ExecuteReaderAsync();

        return await reader.ReadAsync()
            ? MapearVenta(reader)
            : null;
    }

    public async Task<Venta?> ObtenerPorPublicIdAsync(
        Guid publicId,
        IDbConnection connection,
        IDbTransaction transaction)
    {
        var (dbConnection, dbTransaction) =
            ObtenerConexionYTransaccion(connection, transaction);

        await using var command = dbConnection.CreateCommand();
        command.Transaction = dbTransaction;

        command.CommandText = @"
            SELECT VentaId, PublicId, ClienteId, Estado,
                   FechaCreacion, FechaAnulacion,
                   UsuarioCreacionId, UsuarioAnulacionId
            FROM Venta WITH (UPDLOCK, ROWLOCK)
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

    public async Task<IReadOnlyList<DetalleVenta>> ObtenerDetallePorVentaIdAsync(
        int ventaId,
        IDbConnection connection,
        IDbTransaction transaction)
    {
        var (dbConnection, dbTransaction) =
            ObtenerConexionYTransaccion(connection, transaction);

        var detalles = new List<DetalleVenta>();

        await using var command = dbConnection.CreateCommand();
        command.Transaction = dbTransaction;

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

    public async Task<IReadOnlyList<VentaListItemModel>> ObtenerListadoAsync(
        FiltroVentasInput filtro)
    {
        var items = new List<VentaListItemModel>();

        await using var connection = await CrearConexionAbiertaAsync();
        await using var command = connection.CreateCommand();

        var query = @"
            SELECT v.VentaId, v.PublicId, v.Estado, v.FechaCreacion, v.FechaAnulacion,
                   c.RazonSocial AS ClienteNombre, c.CiNit AS ClienteCiNit,
                   u.NombreCompleto AS CreadoPor,
                   ua.NombreCompleto AS AnuladoPor,
                   ISNULL(SUM(d.Importe), 0) AS Total
            FROM Venta v
            INNER JOIN Cliente c ON v.ClienteId = c.ClienteId
            INNER JOIN Usuario u ON v.UsuarioCreacionId = u.UsuarioId
            LEFT JOIN Usuario ua ON v.UsuarioAnulacionId = ua.UsuarioId
            LEFT JOIN DetalleVenta d ON v.VentaId = d.VentaId
            WHERE 1 = 1";

        if (!string.IsNullOrWhiteSpace(filtro.Estado) && !filtro.Estado.Equals("Todas", StringComparison.OrdinalIgnoreCase))
        {
            query += " AND v.Estado = @Estado";
            AgregarParametro(command, "@Estado", filtro.Estado);
        }

        if (filtro.FechaDesde.HasValue)
        {
            var inicioDia = filtro.FechaDesde.Value.ToDateTime(TimeOnly.MinValue);
            query += " AND v.FechaCreacion >= @FechaDesde";
            AgregarParametro(command, "@FechaDesde", inicioDia);
        }

        if (filtro.FechaHasta.HasValue)
        {
            var finDia = filtro.FechaHasta.Value.ToDateTime(TimeOnly.MaxValue);
            query += " AND v.FechaCreacion <= @FechaHasta";
            AgregarParametro(command, "@FechaHasta", finDia);
        }

        query += @"
            GROUP BY v.VentaId, v.PublicId, v.Estado, v.FechaCreacion, v.FechaAnulacion,
                     c.RazonSocial, c.CiNit, u.NombreCompleto, ua.NombreCompleto
            ORDER BY v.FechaCreacion DESC, v.VentaId DESC";

        command.CommandText = query;

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            items.Add(new VentaListItemModel
            {
                PublicId = reader.GetGuid(reader.GetOrdinal("PublicId")),
                FechaCreacion = reader.GetDateTime(reader.GetOrdinal("FechaCreacion")),
                FechaAnulacion = reader.IsDBNull(reader.GetOrdinal("FechaAnulacion"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("FechaAnulacion")),
                ClienteNombre = reader.GetString(reader.GetOrdinal("ClienteNombre")),
                ClienteCiNit = reader.GetString(reader.GetOrdinal("ClienteCiNit")),
                Total = reader.GetDecimal(reader.GetOrdinal("Total")),
                Estado = reader.GetString(reader.GetOrdinal("Estado")),
                CreadoPor = reader.GetString(reader.GetOrdinal("CreadoPor")),
                AnuladoPor = reader.IsDBNull(reader.GetOrdinal("AnuladoPor"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("AnuladoPor"))
            });
        }

        return items;
    }

    public async Task<VentaDetalleCompletoModel?> ObtenerDetalleCompletoAsync(
        Guid publicId,
        bool esAdministrador)
    {
        await using var connection = await CrearConexionAbiertaAsync();

        int ventaId;
        VentaDetalleCompletoModel detalleModel;

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
                SELECT v.VentaId, v.PublicId, v.Estado, v.FechaCreacion, v.FechaAnulacion,
                       c.RazonSocial AS ClienteNombre, c.CiNit AS ClienteCiNit,
                       u.NombreCompleto AS CreadoPor,
                       ua.NombreCompleto AS AnuladoPor
                FROM Venta v
                INNER JOIN Cliente c ON v.ClienteId = c.ClienteId
                INNER JOIN Usuario u ON v.UsuarioCreacionId = u.UsuarioId
                LEFT JOIN Usuario ua ON v.UsuarioAnulacionId = ua.UsuarioId
                WHERE v.PublicId = @PublicId";

            AgregarParametro(command, "@PublicId", publicId);

            await using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return null;
            }

            ventaId = reader.GetInt32(reader.GetOrdinal("VentaId"));
            detalleModel = new VentaDetalleCompletoModel
            {
                PublicId = reader.GetGuid(reader.GetOrdinal("PublicId")),
                FechaCreacion = reader.GetDateTime(reader.GetOrdinal("FechaCreacion")),
                FechaAnulacion = reader.IsDBNull(reader.GetOrdinal("FechaAnulacion"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("FechaAnulacion")),
                Estado = reader.GetString(reader.GetOrdinal("Estado")),
                ClienteNombre = reader.GetString(reader.GetOrdinal("ClienteNombre")),
                ClienteCiNit = reader.GetString(reader.GetOrdinal("ClienteCiNit")),
                CreadoPor = reader.GetString(reader.GetOrdinal("CreadoPor")),
                AnuladoPor = reader.IsDBNull(reader.GetOrdinal("AnuladoPor"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("AnuladoPor"))
            };
        }

        await using (var commandDetalle = connection.CreateCommand())
        {
            commandDetalle.CommandText = @"
                SELECT d.Cantidad, d.PrecioUnitarioVenta, d.Importe, d.Ganancia,
                       p.Nombre AS ProductoNombre
                FROM DetalleVenta d
                INNER JOIN Producto p ON d.ProductoId = p.ProductoId
                WHERE d.VentaId = @VentaId
                ORDER BY d.DetalleVentaId";

            AgregarParametro(commandDetalle, "@VentaId", ventaId);

            await using var readerDetalle = await commandDetalle.ExecuteReaderAsync();

            decimal totalVenta = 0m;
            decimal totalGanancia = 0m;

            while (await readerDetalle.ReadAsync())
            {
                var cantidad = readerDetalle.GetInt32(readerDetalle.GetOrdinal("Cantidad"));
                var precioUnitario = readerDetalle.GetDecimal(readerDetalle.GetOrdinal("PrecioUnitarioVenta"));
                var importe = readerDetalle.GetDecimal(readerDetalle.GetOrdinal("Importe"));
                var ganancia = readerDetalle.GetDecimal(readerDetalle.GetOrdinal("Ganancia"));
                var productoNombre = readerDetalle.GetString(readerDetalle.GetOrdinal("ProductoNombre"));

                totalVenta += importe;
                totalGanancia += ganancia;

                detalleModel.Lineas.Add(new VentaLineaDetalleModel
                {
                    ProductoNombre = productoNombre,
                    Cantidad = cantidad,
                    PrecioUnitario = precioUnitario,
                    Subtotal = importe,
                    Ganancia = esAdministrador ? ganancia : null
                });
            }

            detalleModel.Total = totalVenta;
            detalleModel.GananciaTotal = esAdministrador ? totalGanancia : null;
        }

        return detalleModel;
    }

    public async Task<ComprobanteVenta?> ObtenerComprobanteAsync(Guid publicId)
    {
        await using var connection = await CrearConexionAbiertaAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT v.PublicId, v.Estado,
                   c.CiNit AS CiNitCliente, c.RazonSocial AS RazonSocialCliente,
                   p.Nombre AS NombreProducto,
                   d.Cantidad, d.PrecioUnitarioVenta, d.Importe
            FROM Venta v
            INNER JOIN Cliente c ON c.ClienteId = v.ClienteId
            INNER JOIN DetalleVenta d ON d.VentaId = v.VentaId
            INNER JOIN Producto p ON p.ProductoId = d.ProductoId
            WHERE v.PublicId = @PublicId
            ORDER BY d.DetalleVentaId";

        AgregarParametro(command, "@PublicId", publicId);
        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        var referencia = reader.GetGuid(reader.GetOrdinal("PublicId"));
        var estado = reader.GetString(reader.GetOrdinal("Estado"));
        var ciNit = reader.GetString(reader.GetOrdinal("CiNitCliente"));
        var razonSocial = reader.GetString(reader.GetOrdinal("RazonSocialCliente"));
        var detalles = new List<DetalleComprobanteVenta>();

        do
        {
            detalles.Add(new DetalleComprobanteVenta(
                reader.GetString(reader.GetOrdinal("NombreProducto")),
                reader.GetInt32(reader.GetOrdinal("Cantidad")),
                reader.GetDecimal(reader.GetOrdinal("PrecioUnitarioVenta")),
                reader.GetDecimal(reader.GetOrdinal("Importe"))));
        }
        while (await reader.ReadAsync());

        return new ComprobanteVenta
        {
            PublicId = referencia,
            Estado = estado,
            CiNitCliente = ciNit,
            RazonSocialCliente = razonSocial,
            Detalles = detalles.ToArray()
        };
    }

    private async Task<DbConnection> CrearConexionAbiertaAsync()
    {
        var connection = _connectionFactory.CreateConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        return (DbConnection)connection;
    }

    private static Venta MapearVenta(DbDataReader reader)
    {
        return new Venta
        {
            VentaId = reader.GetInt32(reader.GetOrdinal("VentaId")),
            PublicId = reader.GetGuid(reader.GetOrdinal("PublicId")),
            ClienteId = reader.GetInt32(reader.GetOrdinal("ClienteId")),
            Estado = reader.GetString(reader.GetOrdinal("Estado")),
            FechaCreacion = reader.IsDBNull(reader.GetOrdinal("FechaCreacion"))
                ? DateTime.MinValue
                : reader.GetDateTime(reader.GetOrdinal("FechaCreacion")),
            FechaAnulacion = reader.IsDBNull(reader.GetOrdinal("FechaAnulacion"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("FechaAnulacion")),
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

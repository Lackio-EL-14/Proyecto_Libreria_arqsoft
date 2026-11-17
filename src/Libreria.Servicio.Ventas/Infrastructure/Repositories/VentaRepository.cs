using System.Data;
using System.Data.Common;
using Libreria.Servicio.Ventas.Application.Models;
using Libreria.Servicio.Ventas.Application.Ports.Secondary;
using Libreria.Servicio.Ventas.Domain;

namespace Libreria.Servicio.Ventas.Infrastructure;

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

        venta.PublicId = venta.PublicId == Guid.Empty
            ? Guid.NewGuid()
            : venta.PublicId;

        await using var command = dbConnection.CreateCommand();
        command.Transaction = dbTransaction;
        command.CommandText = @"
            INSERT INTO dbo.Venta
                (PublicId, ClientePublicId, ClienteCiNit, ClienteRazonSocial,
                 Total, Estado, Observacion, VendedorIniciales,
                 FechaCreacion, UsuarioCreacionId)
            OUTPUT INSERTED.VentaId
            VALUES
                (@PublicId, @ClientePublicId, @ClienteCiNit, @ClienteRazonSocial,
                 @Total, @Estado, @Observacion, @VendedorIniciales,
                 SYSDATETIME(), @UsuarioCreacionId)";

        AgregarParametro(command, "@PublicId", venta.PublicId);
        AgregarParametro(command, "@ClientePublicId", venta.ClientePublicId);
        AgregarParametro(command, "@ClienteCiNit", venta.ClienteCiNit);
        AgregarParametro(command, "@ClienteRazonSocial", venta.ClienteRazonSocial);
        AgregarParametro(command, "@Total", venta.Total);
        AgregarParametro(command, "@Estado", venta.Estado);
        AgregarParametro(command, "@Observacion", DbValor(venta.Observacion));
        AgregarParametro(command, "@VendedorIniciales", DbValor(venta.VendedorIniciales));
        AgregarParametro(command, "@UsuarioCreacionId", venta.UsuarioCreacionId);

        venta.VentaId = Convert.ToInt32(await command.ExecuteScalarAsync());
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
            INSERT INTO dbo.DetalleVenta
                (VentaId, ProductoPublicId, ProductoNombre, Cantidad,
                 PrecioUnitarioVenta, CostoAdquisicionUnitario, Importe, Ganancia)
            VALUES
                (@VentaId, @ProductoPublicId, @ProductoNombre, @Cantidad,
                 @PrecioUnitarioVenta, @CostoAdquisicionUnitario, @Importe, @Ganancia)";

        AgregarParametro(command, "@VentaId", detalle.VentaId);
        AgregarParametro(command, "@ProductoPublicId", detalle.ProductoPublicId);
        AgregarParametro(command, "@ProductoNombre", detalle.ProductoNombre);
        AgregarParametro(command, "@Cantidad", detalle.Cantidad);
        AgregarParametro(command, "@PrecioUnitarioVenta", detalle.PrecioUnitarioVenta);
        AgregarParametro(command, "@CostoAdquisicionUnitario", detalle.CostoAdquisicionUnitario);
        AgregarParametro(command, "@Importe", detalle.Importe);
        AgregarParametro(command, "@Ganancia", detalle.Ganancia);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<bool> ActualizarEstadoAsync(
        int ventaId,
        string estado,
        Guid? usuarioAnulacionId,
        string? motivoAnulacion,
        IDbConnection connection,
        IDbTransaction transaction)
    {
        var (dbConnection, dbTransaction) =
            ObtenerConexionYTransaccion(connection, transaction);

        await using var command = dbConnection.CreateCommand();
        command.Transaction = dbTransaction;
        command.CommandText = @"
            UPDATE dbo.Venta
            SET Estado = @Estado,
                UsuarioAnulacionId = @UsuarioAnulacionId,
                MotivoAnulacion = @MotivoAnulacion,
                FechaAnulacion = CASE
                    WHEN @Estado = N'Anulada' THEN SYSDATETIME()
                    ELSE FechaAnulacion
                END
            WHERE VentaId = @VentaId";

        AgregarParametro(command, "@VentaId", ventaId);
        AgregarParametro(command, "@Estado", estado);
        AgregarParametro(command, "@UsuarioAnulacionId", DbValor(usuarioAnulacionId));
        AgregarParametro(command, "@MotivoAnulacion", DbValor(motivoAnulacion));

        return await command.ExecuteNonQueryAsync() == 1;
    }

    public async Task<bool> ActualizarDatosAsync(
        int ventaId,
        ClienteVentaSnapshot cliente,
        string? observacion,
        Guid usuarioModificacionId,
        IDbConnection connection,
        IDbTransaction transaction)
    {
        var (dbConnection, dbTransaction) =
            ObtenerConexionYTransaccion(connection, transaction);

        await using var command = dbConnection.CreateCommand();
        command.Transaction = dbTransaction;
        command.CommandText = @"
            UPDATE dbo.Venta
            SET ClientePublicId = @ClientePublicId,
                ClienteCiNit = @ClienteCiNit,
                ClienteRazonSocial = @ClienteRazonSocial,
                Observacion = @Observacion,
                UsuarioModificacionId = @UsuarioModificacionId,
                FechaModificacion = SYSDATETIME()
            WHERE VentaId = @VentaId
              AND Estado = N'Activa'";

        AgregarParametro(command, "@VentaId", ventaId);
        AgregarParametro(command, "@ClientePublicId", cliente.PublicId);
        AgregarParametro(command, "@ClienteCiNit", cliente.CiNit);
        AgregarParametro(command, "@ClienteRazonSocial", cliente.RazonSocial);
        AgregarParametro(command, "@Observacion", DbValor(observacion));
        AgregarParametro(command, "@UsuarioModificacionId", usuarioModificacionId);

        return await command.ExecuteNonQueryAsync() == 1;
    }

    public Task<IReadOnlyList<Venta>> ObtenerActivasAsync() =>
        ObtenerVentasAsync(soloActivas: true);

    public Task<IReadOnlyList<Venta>> ObtenerTodasAsync() =>
        ObtenerVentasAsync(soloActivas: false);

    public async Task<Venta?> ObtenerPorPublicIdAsync(Guid publicId)
    {
        await using var connection = await CrearConexionAbiertaAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = SeleccionVenta + @"
            WHERE PublicId = @PublicId";

        AgregarParametro(command, "@PublicId", publicId);
        await using var reader = await command.ExecuteReaderAsync();

        return await reader.ReadAsync() ? MapearVenta(reader) : null;
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
        command.CommandText = SeleccionVenta + @"
            WITH (UPDLOCK, ROWLOCK)
            WHERE PublicId = @PublicId";

        AgregarParametro(command, "@PublicId", publicId);
        await using var reader = await command.ExecuteReaderAsync();

        return await reader.ReadAsync() ? MapearVenta(reader) : null;
    }

    public async Task<IReadOnlyList<DetalleVenta>> ObtenerDetallePorVentaIdAsync(
        int ventaId)
    {
        await using var connection = await CrearConexionAbiertaAsync();
        return await ObtenerDetallesAsync(ventaId, connection, transaction: null);
    }

    public Task<IReadOnlyList<DetalleVenta>> ObtenerDetallePorVentaIdAsync(
        int ventaId,
        IDbConnection connection,
        IDbTransaction transaction)
    {
        var (dbConnection, dbTransaction) =
            ObtenerConexionYTransaccion(connection, transaction);

        return ObtenerDetallesAsync(ventaId, dbConnection, dbTransaction);
    }

    public async Task<IReadOnlyList<VentaListItemModel>> ObtenerListadoAsync(
        FiltroVentasInput filtro,
        bool esAdministrador)
    {
        var items = new List<VentaListItemModel>();

        await using var connection = await CrearConexionAbiertaAsync();
        await using var command = connection.CreateCommand();

        var query = @"
            SELECT v.PublicId, v.FechaCreacion, v.FechaAnulacion,
                   v.ClienteRazonSocial AS ClienteNombre,
                   v.ClienteCiNit, v.Total, v.Estado,
                   CASE WHEN @EsAdministrador = 1
                       THEN (SELECT COALESCE(SUM(d.Ganancia), 0)
                             FROM dbo.DetalleVenta d
                             WHERE d.VentaId = v.VentaId)
                       ELSE NULL
                   END AS GananciaTotal,
                   COALESCE(NULLIF(v.VendedorIniciales, N''),
                            CONVERT(nvarchar(36), v.UsuarioCreacionId)) AS CreadoPor,
                   CASE
                       WHEN v.UsuarioAnulacionId IS NULL THEN NULL
                       ELSE CONVERT(nvarchar(36), v.UsuarioAnulacionId)
                   END AS AnuladoPor
            FROM dbo.Venta v
            WHERE 1 = 1";

        AgregarParametro(command, "@EsAdministrador", esAdministrador);

        if (!string.IsNullOrWhiteSpace(filtro.Estado)
            && !filtro.Estado.Equals("Todas", StringComparison.OrdinalIgnoreCase))
        {
            query += " AND v.Estado = @Estado";
            AgregarParametro(command, "@Estado", filtro.Estado);
        }

        if (filtro.FechaDesde.HasValue)
        {
            query += " AND v.FechaCreacion >= @FechaDesde";
            AgregarParametro(
                command,
                "@FechaDesde",
                filtro.FechaDesde.Value.ToDateTime(TimeOnly.MinValue));
        }

        if (filtro.FechaHasta.HasValue)
        {
            query += " AND v.FechaCreacion <= @FechaHasta";
            AgregarParametro(
                command,
                "@FechaHasta",
                filtro.FechaHasta.Value.ToDateTime(TimeOnly.MaxValue));
        }

        var columnaOrden = filtro.Orden == OrdenVentas.Total
            ? "v.Total"
            : "v.FechaCreacion";
        var direccion = filtro.Descendente ? "DESC" : "ASC";

        command.CommandText = query
            + $" ORDER BY {columnaOrden} {direccion}, v.VentaId {direccion}";

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(new VentaListItemModel
            {
                PublicId = reader.GetGuid(reader.GetOrdinal("PublicId")),
                FechaCreacion = reader.GetDateTime(reader.GetOrdinal("FechaCreacion")),
                FechaAnulacion = GetNullableDateTime(reader, "FechaAnulacion"),
                ClienteNombre = reader.GetString(reader.GetOrdinal("ClienteNombre")),
                ClienteCiNit = reader.GetString(reader.GetOrdinal("ClienteCiNit")),
                Total = reader.GetDecimal(reader.GetOrdinal("Total")),
                GananciaTotal = GetNullableDecimal(reader, "GananciaTotal"),
                Estado = reader.GetString(reader.GetOrdinal("Estado")),
                CreadoPor = reader.GetString(reader.GetOrdinal("CreadoPor")),
                AnuladoPor = GetNullableString(reader, "AnuladoPor")
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
        VentaDetalleCompletoModel detalle;

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
                SELECT v.VentaId, v.PublicId, v.FechaCreacion, v.FechaAnulacion,
                       v.MotivoAnulacion, v.Observacion, v.Estado, v.ClienteRazonSocial,
                       v.ClienteCiNit, v.Total,
                       v.FechaModificacion,
                       CASE
                           WHEN v.UsuarioModificacionId IS NULL THEN NULL
                           ELSE CONVERT(nvarchar(36), v.UsuarioModificacionId)
                       END AS ModificadoPor,
                       COALESCE(NULLIF(v.VendedorIniciales, N''),
                                CONVERT(nvarchar(36), v.UsuarioCreacionId)) AS CreadoPor,
                       CASE
                           WHEN v.UsuarioAnulacionId IS NULL THEN NULL
                           ELSE CONVERT(nvarchar(36), v.UsuarioAnulacionId)
                       END AS AnuladoPor
                FROM dbo.Venta v
                WHERE v.PublicId = @PublicId";

            AgregarParametro(command, "@PublicId", publicId);
            await using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return null;
            }

            ventaId = reader.GetInt32(reader.GetOrdinal("VentaId"));
            detalle = new VentaDetalleCompletoModel
            {
                PublicId = reader.GetGuid(reader.GetOrdinal("PublicId")),
                FechaCreacion = reader.GetDateTime(reader.GetOrdinal("FechaCreacion")),
                FechaAnulacion = GetNullableDateTime(reader, "FechaAnulacion"),
                MotivoAnulacion = GetNullableString(reader, "MotivoAnulacion"),
                Observacion = GetNullableString(reader, "Observacion"),
                Estado = reader.GetString(reader.GetOrdinal("Estado")),
                ClienteNombre = reader.GetString(reader.GetOrdinal("ClienteRazonSocial")),
                ClienteCiNit = reader.GetString(reader.GetOrdinal("ClienteCiNit")),
                CreadoPor = reader.GetString(reader.GetOrdinal("CreadoPor")),
                AnuladoPor = GetNullableString(reader, "AnuladoPor"),
                ModificadoPor = GetNullableString(reader, "ModificadoPor"),
                FechaModificacion = GetNullableDateTime(reader, "FechaModificacion"),
                Total = reader.GetDecimal(reader.GetOrdinal("Total"))
            };
        }

        await using var detalleCommand = connection.CreateCommand();
        detalleCommand.CommandText = @"
            SELECT ProductoNombre, Cantidad, PrecioUnitarioVenta, Importe, Ganancia
            FROM dbo.DetalleVenta
            WHERE VentaId = @VentaId
            ORDER BY DetalleVentaId";

        AgregarParametro(detalleCommand, "@VentaId", ventaId);
        await using var detalleReader = await detalleCommand.ExecuteReaderAsync();

        decimal gananciaTotal = 0m;
        while (await detalleReader.ReadAsync())
        {
            var ganancia = detalleReader.GetDecimal(detalleReader.GetOrdinal("Ganancia"));
            gananciaTotal += ganancia;

            detalle.Lineas.Add(new VentaLineaDetalleModel
            {
                ProductoNombre = detalleReader.GetString(
                    detalleReader.GetOrdinal("ProductoNombre")),
                Cantidad = detalleReader.GetInt32(detalleReader.GetOrdinal("Cantidad")),
                PrecioUnitario = detalleReader.GetDecimal(
                    detalleReader.GetOrdinal("PrecioUnitarioVenta")),
                Subtotal = detalleReader.GetDecimal(detalleReader.GetOrdinal("Importe")),
                Ganancia = esAdministrador ? ganancia : null
            });
        }

        detalle.GananciaTotal = esAdministrador ? gananciaTotal : null;
        return detalle;
    }

    public async Task<ComprobanteVenta?> ObtenerComprobanteAsync(Guid publicId)
    {
        await using var connection = await CrearConexionAbiertaAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT v.PublicId, v.Estado, v.Total, v.FechaCreacion,
                   v.ClienteCiNit, v.ClienteRazonSocial,
                   d.ProductoNombre, d.Cantidad, d.PrecioUnitarioVenta, d.Importe
            FROM dbo.Venta v
            INNER JOIN dbo.DetalleVenta d ON d.VentaId = v.VentaId
            WHERE v.PublicId = @PublicId
            ORDER BY d.DetalleVentaId";

        AgregarParametro(command, "@PublicId", publicId);
        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        var detalles = new List<DetalleComprobanteVenta>();
        var comprobante = new ComprobanteVenta
        {
            PublicId = reader.GetGuid(reader.GetOrdinal("PublicId")),
            Estado = reader.GetString(reader.GetOrdinal("Estado")),
            CiNitCliente = reader.GetString(reader.GetOrdinal("ClienteCiNit")),
            RazonSocialCliente = reader.GetString(reader.GetOrdinal("ClienteRazonSocial")),
            Total = reader.GetDecimal(reader.GetOrdinal("Total")),
            FechaVenta = new DateTimeOffset(
                DateTime.SpecifyKind(
                    reader.GetDateTime(reader.GetOrdinal("FechaCreacion")),
                    DateTimeKind.Utc))
        };

        do
        {
            detalles.Add(new DetalleComprobanteVenta(
                reader.GetString(reader.GetOrdinal("ProductoNombre")),
                reader.GetInt32(reader.GetOrdinal("Cantidad")),
                reader.GetDecimal(reader.GetOrdinal("PrecioUnitarioVenta")),
                reader.GetDecimal(reader.GetOrdinal("Importe"))));
        }
        while (await reader.ReadAsync());

        return comprobante with { Detalles = detalles };
    }

    private async Task<IReadOnlyList<Venta>> ObtenerVentasAsync(bool soloActivas)
    {
        var ventas = new List<Venta>();

        await using var connection = await CrearConexionAbiertaAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = SeleccionVenta
            + (soloActivas ? " WHERE Estado = N'Activa'" : string.Empty)
            + " ORDER BY FechaCreacion DESC, VentaId DESC";

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            ventas.Add(MapearVenta(reader));
        }

        return ventas;
    }

    private static async Task<IReadOnlyList<DetalleVenta>> ObtenerDetallesAsync(
        int ventaId,
        DbConnection connection,
        DbTransaction? transaction)
    {
        var detalles = new List<DetalleVenta>();

        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
            SELECT DetalleVentaId, VentaId, ProductoPublicId, ProductoNombre,
                   Cantidad, PrecioUnitarioVenta, CostoAdquisicionUnitario,
                   Importe, Ganancia
            FROM dbo.DetalleVenta
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

    private static Venta MapearVenta(DbDataReader reader) =>
        new()
        {
            VentaId = reader.GetInt32(reader.GetOrdinal("VentaId")),
            PublicId = reader.GetGuid(reader.GetOrdinal("PublicId")),
            ClientePublicId = reader.GetGuid(reader.GetOrdinal("ClientePublicId")),
            ClienteCiNit = reader.GetString(reader.GetOrdinal("ClienteCiNit")),
            ClienteRazonSocial = reader.GetString(reader.GetOrdinal("ClienteRazonSocial")),
            Total = reader.GetDecimal(reader.GetOrdinal("Total")),
            Estado = reader.GetString(reader.GetOrdinal("Estado")),
            Observacion = GetNullableString(reader, "Observacion"),
            MotivoAnulacion = GetNullableString(reader, "MotivoAnulacion"),
            VendedorIniciales = GetNullableString(reader, "VendedorIniciales"),
            FechaCreacion = reader.GetDateTime(reader.GetOrdinal("FechaCreacion")),
            UsuarioCreacionId = reader.GetGuid(reader.GetOrdinal("UsuarioCreacionId")),
            FechaModificacion = GetNullableDateTime(reader, "FechaModificacion"),
            UsuarioModificacionId = GetNullableGuid(reader, "UsuarioModificacionId"),
            FechaAnulacion = GetNullableDateTime(reader, "FechaAnulacion"),
            UsuarioAnulacionId = GetNullableGuid(reader, "UsuarioAnulacionId")
        };

    private static DetalleVenta MapearDetalle(DbDataReader reader) =>
        new()
        {
            DetalleVentaId = reader.GetInt32(reader.GetOrdinal("DetalleVentaId")),
            VentaId = reader.GetInt32(reader.GetOrdinal("VentaId")),
            ProductoPublicId = reader.GetGuid(reader.GetOrdinal("ProductoPublicId")),
            ProductoNombre = reader.GetString(reader.GetOrdinal("ProductoNombre")),
            Cantidad = reader.GetInt32(reader.GetOrdinal("Cantidad")),
            PrecioUnitarioVenta = reader.GetDecimal(
                reader.GetOrdinal("PrecioUnitarioVenta")),
            CostoAdquisicionUnitario = reader.GetDecimal(
                reader.GetOrdinal("CostoAdquisicionUnitario")),
            Importe = reader.GetDecimal(reader.GetOrdinal("Importe")),
            Ganancia = reader.GetDecimal(reader.GetOrdinal("Ganancia"))
        };

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

    private static string? GetNullableString(DbDataReader reader, string columna)
    {
        var ordinal = reader.GetOrdinal(columna);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static DateTime? GetNullableDateTime(DbDataReader reader, string columna)
    {
        var ordinal = reader.GetOrdinal(columna);
        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    }

    private static Guid? GetNullableGuid(DbDataReader reader, string columna)
    {
        var ordinal = reader.GetOrdinal(columna);
        return reader.IsDBNull(ordinal) ? null : reader.GetGuid(ordinal);
    }

    private static decimal? GetNullableDecimal(DbDataReader reader, string columna)
    {
        var ordinal = reader.GetOrdinal(columna);
        return reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
    }

    private static object DbValor(object? valor) => valor ?? DBNull.Value;

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

    private const string SeleccionVenta = @"
        SELECT VentaId, PublicId, ClientePublicId, ClienteCiNit,
               ClienteRazonSocial, Total, Estado, Observacion,
               MotivoAnulacion, VendedorIniciales, FechaCreacion,
               UsuarioCreacionId, FechaModificacion, UsuarioModificacionId,
               FechaAnulacion, UsuarioAnulacionId
        FROM dbo.Venta";
}

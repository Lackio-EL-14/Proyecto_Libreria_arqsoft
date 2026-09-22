using Libreria.Web.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Libreria.Web.Data;
using System.Data;
using System.Data.Common;

namespace Libreria.Web.Data.Repositories
{
    public class ProductoRepository : ICrudRepository<Producto>
    {

        private readonly IDbConnectionFactory _connectionFactory;

        public ProductoRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IReadOnlyList<Producto>> ObtenerActivasAsync(string? busqueda = null)
        {
            var productos = new List<Producto>();

            await using var connection = await CrearConexionAbiertaAsync();
            await using var command = connection.CreateCommand();

            command.CommandText = @"
        SELECT ProductoId, PublicId, Nombre, DescripcionEspecifica,
               EsPerecedero, FechaVencimiento, Stock, PrecioVenta,
               CostoAdquisicionActual, CategoriaId, MarcaId,
               Estado, FechaCreacion, FechaModificacion
        FROM Producto
        WHERE Estado = 1
          AND (@Busqueda IS NULL
               OR Nombre LIKE '%' + @Busqueda + '%'
               OR DescripcionEspecifica LIKE '%' + @Busqueda + '%')
        ORDER BY Nombre";

            AgregarParametro(
                command,
                "@Busqueda",
                string.IsNullOrWhiteSpace(busqueda)
                    ? DBNull.Value
                    : busqueda.Trim());

            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                productos.Add(MapearProducto(reader));
            }

            return productos;
        }

        public async Task<Producto?> ObtenerPorPublicIdAsync(
         Guid publicId,
         bool estadoEsperado)
        {
            await using var connection = await CrearConexionAbiertaAsync();
            await using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT ProductoId, PublicId, Nombre, DescripcionEspecifica,
                       EsPerecedero, FechaVencimiento, Stock, PrecioVenta,
                       CostoAdquisicionActual, CategoriaId, MarcaId,
                       Estado, FechaCreacion, FechaModificacion
                FROM Producto
                WHERE PublicId = @PublicId
                  AND Estado = @Estado";

            AgregarParametro(command, "@PublicId", publicId);
            AgregarParametro(command, "@Estado", estadoEsperado);

            await using var reader = await command.ExecuteReaderAsync();

            return await reader.ReadAsync()
                ? MapearProducto(reader)
                : null;
        }

        public async Task CrearAsync(Producto entidad)
        {
            await using var connection = await CrearConexionAbiertaAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                var productoId = await InsertarProductoAsync(connection, transaction, entidad);

                await InsertarHistoricoAsync(
                    connection,
                    transaction,
                    productoId,
                    entidad.CostoAdquisicionActual,
                    entidad.FechaVencimiento,
                    "Registro inicial");

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> ActualizarAsync(Producto entidad)
        {
            await using var connection = await CrearConexionAbiertaAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                var datosActuales = await ObtenerDatosActualesAsync(
                    connection,
                    transaction,
                    entidad.PublicId);

                if (!datosActuales.HasValue)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                await using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;

                    command.CommandText = @"
                        UPDATE Producto
                        SET Nombre = @Nombre,
                            DescripcionEspecifica = @DescripcionEspecifica,
                            EsPerecedero = @EsPerecedero,
                            FechaVencimiento = @FechaVencimiento,
                            Stock = @Stock,
                            PrecioVenta = @PrecioVenta,
                            CostoAdquisicionActual = @CostoAdquisicionActual,
                            CategoriaId = @CategoriaId,
                            MarcaId = @MarcaId,
                            FechaModificacion = SYSDATETIME()
                        WHERE PublicId = @PublicId
                          AND Estado = 1";

                    AgregarDatosProducto(command, entidad);
                    AgregarParametro(command, "@PublicId", entidad.PublicId);

                    if (await command.ExecuteNonQueryAsync() != 1)
                    {
                        await transaction.RollbackAsync();
                        return false;
                    }
                }

                if (datosActuales.Value.CostoActual != entidad.CostoAdquisicionActual)
                {
                    await InsertarHistoricoAsync(
                        connection,
                        transaction,
                        datosActuales.Value.ProductoId,
                        entidad.CostoAdquisicionActual,
                        entidad.FechaVencimiento,
                        "Edición manual");
                }

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> CambiarEstadoAsync(Guid publicId, bool nuevoEstado)
        {
            bool estadoEsperado = !nuevoEstado;

            await using var connection = await CrearConexionAbiertaAsync();
            await using var command = connection.CreateCommand();

            command.CommandText = @"
                UPDATE Producto
                SET Estado = @NuevoEstado,
                    FechaModificacion = SYSDATETIME()
                WHERE PublicId = @PublicId
                  AND Estado = @EstadoEsperado";

            AgregarParametro(command, "@PublicId", publicId);
            AgregarParametro(command, "@NuevoEstado", nuevoEstado);
            AgregarParametro(command, "@EstadoEsperado", estadoEsperado);

            return await command.ExecuteNonQueryAsync() == 1;
        }

        public async Task<bool> ExisteValorAsync(
            string columna,
            string valor,
            Guid? excluirPublicId = null)
        {
            await using var connection = await CrearConexionAbiertaAsync();
            await using var command = connection.CreateCommand();

            command.CommandText = $@"
                SELECT COUNT(1)
                FROM Producto
                WHERE {columna} = @Valor
                  AND (@ExcluirId IS NULL OR PublicId <> @ExcluirId)";

            AgregarParametro(command, "@Valor", valor);
            AgregarParametro(
                command,
                "@ExcluirId",
                excluirPublicId ?? (object)DBNull.Value);

            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        public async Task<bool> TieneRelacionesAsync(Guid publicId)
        {
            await using var connection = await CrearConexionAbiertaAsync();
            await using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT COUNT(1)
                FROM HistoricoCostoProducto h
                INNER JOIN Producto p ON h.ProductoId = p.ProductoId
                WHERE p.PublicId = @PublicId";

            AgregarParametro(command, "@PublicId", publicId);

            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
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

        private static Producto MapearProducto(DbDataReader reader)
        {
            return new Producto
            {
                ProductoId = reader.GetInt32(reader.GetOrdinal("ProductoId")),
                PublicId = reader.GetGuid(reader.GetOrdinal("PublicId")),
                Nombre = reader.GetString(reader.GetOrdinal("Nombre")),

                DescripcionEspecifica =
                    reader.IsDBNull(reader.GetOrdinal("DescripcionEspecifica"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("DescripcionEspecifica")),
                EsPerecedero = reader.GetBoolean(
                    reader.GetOrdinal("EsPerecedero")),

                FechaVencimiento =
                    reader.IsDBNull(reader.GetOrdinal("FechaVencimiento"))
                        ? null
                        : reader.GetDateTime(reader.GetOrdinal("FechaVencimiento")),

                Stock = reader.GetInt32(reader.GetOrdinal("Stock")),
                PrecioVenta = reader.GetDecimal(reader.GetOrdinal("PrecioVenta")),
                CostoAdquisicionActual = reader.GetDecimal(reader.GetOrdinal("CostoAdquisicionActual")),
                CategoriaId = reader.GetInt32(reader.GetOrdinal("CategoriaId")),
                MarcaId = reader.GetInt32(reader.GetOrdinal("MarcaId")),
                Estado = reader.GetBoolean(reader.GetOrdinal("Estado")),
                FechaCreacion = reader.GetDateTime(reader.GetOrdinal("FechaCreacion")),

                FechaModificacion =
                    reader.IsDBNull(reader.GetOrdinal("FechaModificacion"))
                        ? null
                        : reader.GetDateTime(reader.GetOrdinal("FechaModificacion"))
            };
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

        private static async Task<int> InsertarProductoAsync(
            DbConnection connection,
            DbTransaction transaction,
            Producto producto)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;

            command.CommandText = @"
                INSERT INTO Producto
                    (Nombre, DescripcionEspecifica, EsPerecedero, FechaVencimiento, Stock,
                     PrecioVenta, CostoAdquisicionActual, CategoriaId, MarcaId, Estado)
                VALUES
                    (@Nombre, @DescripcionEspecifica, @EsPerecedero, @FechaVencimiento, @Stock,
                     @PrecioVenta, @CostoAdquisicionActual, @CategoriaId, @MarcaId, 1);

                SELECT CAST(SCOPE_IDENTITY() AS INT)";

            AgregarDatosProducto(command, producto);

            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        private static async Task InsertarHistoricoAsync(
            DbConnection connection,
            DbTransaction transaction,
            int productoId,
            decimal costo,
            DateTime? fechaVencimiento,
            string motivo)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;

            command.CommandText = @"
                INSERT INTO HistoricoCostoProducto
                    (ProductoId, CostoAdquisicion, FechaVencimiento, Motivo)
                VALUES
                    (@ProductoId, @Costo, @FechaVencimiento, @Motivo)";

            AgregarParametro(command, "@ProductoId", productoId);
            AgregarParametro(command, "@Costo", costo);
            AgregarParametro(command, "@FechaVencimiento", fechaVencimiento ?? (object)DBNull.Value);
            AgregarParametro(command, "@Motivo", motivo);

            await command.ExecuteNonQueryAsync();
        }

        private static void AgregarDatosProducto(
            DbCommand command,
            Producto producto)
        {
            AgregarParametro(command, "@Nombre", producto.Nombre);

            AgregarParametro(
                command,
                "@DescripcionEspecifica",
                producto.DescripcionEspecifica ?? (object)DBNull.Value);

            AgregarParametro(command, "@EsPerecedero", producto.EsPerecedero);

            AgregarParametro(
                command,
                "@FechaVencimiento",
                producto.FechaVencimiento ?? (object)DBNull.Value);

            AgregarParametro(command, "@Stock", producto.Stock);
            AgregarParametro(command, "@PrecioVenta", producto.PrecioVenta);
            AgregarParametro(command, "@CostoAdquisicionActual", producto.CostoAdquisicionActual);
            AgregarParametro(command, "@CategoriaId", producto.CategoriaId);
            AgregarParametro(command, "@MarcaId", producto.MarcaId);
        }

        private static async Task<(int ProductoId, decimal CostoActual)?>
            ObtenerDatosActualesAsync(
                DbConnection connection,
                DbTransaction transaction,
                Guid publicId)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;

            command.CommandText = @"
                SELECT ProductoId, CostoAdquisicionActual
                FROM Producto
                WHERE PublicId = @PublicId
                  AND Estado = 1";

            AgregarParametro(command, "@PublicId", publicId);

            await using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return null;
            }

            return (
                reader.GetInt32(0),
                reader.GetDecimal(1)
            );
        }
    }
}
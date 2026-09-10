using System.Data;
using System.Data.Common;
using Libreria.Web.Data;
using Libreria.Web.Pages.Productos.Models;
using Libreria.Web.Pages.Productos.Services;

namespace Libreria.Web.Pages.Productos.Repositories;

public class ProductoRepository : IProductoRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly CostoProductoService _comparadorCosto;

    public ProductoRepository(
        IDbConnectionFactory connectionFactory,
        CostoProductoService comparadorCosto)
    {
        _connectionFactory = connectionFactory;
        _comparadorCosto = comparadorCosto;
    }

    public IReadOnlyList<ProductoListItem> ObtenerProductos(
        string? busqueda,
        int? categoriaId,
        int? marcaId)
    {
        var productos = new List<ProductoListItem>();
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT p.ProductoId, p.Nombre, p.Stock, p.PrecioVenta,
                   p.CostoAdquisicionActual, c.Nombre AS Categoria,
                   m.Nombre AS Marca
            FROM Producto p
            INNER JOIN Categoria c ON p.CategoriaId = c.CategoriaId
            INNER JOIN Marca m ON p.MarcaId = m.MarcaId
            WHERE p.Estado = 1
              AND (@Busqueda IS NULL
                   OR p.Nombre LIKE '%' + @Busqueda + '%'
                   OR p.DescripcionEspecifica LIKE '%' + @Busqueda + '%')
              AND (@CategoriaId IS NULL OR p.CategoriaId = @CategoriaId)
              AND (@MarcaId IS NULL OR p.MarcaId = @MarcaId)
            ORDER BY p.Nombre";
        AgregarParametro(command, "@Busqueda",
            string.IsNullOrWhiteSpace(busqueda) ? DBNull.Value : busqueda.Trim());
        AgregarParametro(command, "@CategoriaId", categoriaId ?? (object)DBNull.Value);
        AgregarParametro(command, "@MarcaId", marcaId ?? (object)DBNull.Value);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            productos.Add(new ProductoListItem(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetInt32(2),
                reader.GetDecimal(3),
                reader.GetDecimal(4),
                reader.GetString(5),
                reader.GetString(6)));
        }

        return productos;
    }

    public ProductoDetalle? ObtenerActivoPorId(int productoId)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT p.ProductoId, p.Nombre, p.DescripcionEspecifica,
                   p.FechaVencimiento, p.Stock, p.PrecioVenta,
                   p.CostoAdquisicionActual, p.CategoriaId,
                   c.Nombre AS Categoria, p.MarcaId, m.Nombre AS Marca
            FROM Producto p
            INNER JOIN Categoria c ON p.CategoriaId = c.CategoriaId
            INNER JOIN Marca m ON p.MarcaId = m.MarcaId
            WHERE p.ProductoId = @ProductoId
              AND p.Estado = 1";
        AgregarParametro(command, "@ProductoId", productoId);
        using var reader = command.ExecuteReader();
        return reader.Read() ? MapearDetalle(reader) : null;
    }

    public int CrearConHistorico(ProductoInput input)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            var productoId = InsertarProducto(connection, transaction, input);
            InsertarHistorico(
                connection,
                transaction,
                productoId,
                input.CostoAdquisicion,
                "Registro inicial");
            transaction.Commit();
            return productoId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public bool ActualizarConHistorico(
        int productoId,
        ProductoInput input,
        string motivo)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            var costoActual = ObtenerCostoActual(connection, transaction, productoId);
            if (!costoActual.HasValue)
            {
                transaction.Rollback();
                return false;
            }

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
                    UPDATE Producto
                    SET Nombre = @Nombre,
                        DescripcionEspecifica = @DescripcionEspecifica,
                        FechaVencimiento = @FechaVencimiento,
                        Stock = @Stock,
                        PrecioVenta = @PrecioVenta,
                        CostoAdquisicionActual = @CostoAdquisicion,
                        CategoriaId = @CategoriaId,
                        MarcaId = @MarcaId,
                        FechaModificacion = SYSDATETIME()
                    WHERE ProductoId = @ProductoId
                      AND Estado = 1";
                AgregarDatosProducto(command, input);
                AgregarParametro(command, "@ProductoId", productoId);
                if (command.ExecuteNonQuery() != 1)
                {
                    transaction.Rollback();
                    return false;
                }
            }

            if (_comparadorCosto.Cambio(costoActual.Value, input.CostoAdquisicion))
            {
                InsertarHistorico(
                    connection,
                    transaction,
                    productoId,
                    input.CostoAdquisicion,
                    motivo);
            }

            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public bool DarDeBaja(int productoId)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE Producto
            SET Estado = 0,
                FechaModificacion = SYSDATETIME()
            WHERE ProductoId = @ProductoId
              AND Estado = 1";
        AgregarParametro(command, "@ProductoId", productoId);
        return command.ExecuteNonQuery() == 1;
    }

    private static int InsertarProducto(
        DbConnection connection,
        DbTransaction transaction,
        ProductoInput input)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
            INSERT INTO Producto
                (Nombre, DescripcionEspecifica, FechaVencimiento, Stock,
                 PrecioVenta, CostoAdquisicionActual, CategoriaId, MarcaId, Estado)
            VALUES
                (@Nombre, @DescripcionEspecifica, @FechaVencimiento, @Stock,
                 @PrecioVenta, @CostoAdquisicion, @CategoriaId, @MarcaId, 1);
            SELECT CAST(SCOPE_IDENTITY() AS INT)";
        AgregarDatosProducto(command, input);
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static decimal? ObtenerCostoActual(
        DbConnection connection,
        DbTransaction transaction,
        int productoId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
            SELECT CostoAdquisicionActual
            FROM Producto
            WHERE ProductoId = @ProductoId
              AND Estado = 1";
        AgregarParametro(command, "@ProductoId", productoId);
        var resultado = command.ExecuteScalar();
        return resultado is null or DBNull ? null : Convert.ToDecimal(resultado);
    }

    private static void InsertarHistorico(
        DbConnection connection,
        DbTransaction transaction,
        int productoId,
        decimal costo,
        string motivo)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
            INSERT INTO HistoricoCostoProducto
                (ProductoId, CostoAdquisicion, Motivo)
            VALUES
                (@ProductoId, @Costo, @Motivo)";
        AgregarParametro(command, "@ProductoId", productoId);
        AgregarParametro(command, "@Costo", costo);
        AgregarParametro(command, "@Motivo", motivo);
        command.ExecuteNonQuery();
    }

    private static ProductoDetalle MapearDetalle(DbDataReader reader)
    {
        return new ProductoDetalle(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetDateTime(3),
            reader.GetInt32(4),
            reader.GetDecimal(5),
            reader.GetDecimal(6),
            reader.GetInt32(7),
            reader.GetString(8),
            reader.GetInt32(9),
            reader.GetString(10));
    }

    private static void AgregarDatosProducto(DbCommand command, ProductoInput input)
    {
        AgregarParametro(command, "@Nombre", input.Nombre);
        AgregarParametro(command, "@DescripcionEspecifica",
            input.DescripcionEspecifica ?? (object)DBNull.Value);
        AgregarParametro(command, "@FechaVencimiento",
            input.FechaVencimiento ?? (object)DBNull.Value);
        AgregarParametro(command, "@Stock", input.Stock);
        AgregarParametro(command, "@PrecioVenta", input.PrecioVenta);
        AgregarParametro(command, "@CostoAdquisicion", input.CostoAdquisicion);
        AgregarParametro(command, "@CategoriaId", input.CategoriaId);
        AgregarParametro(command, "@MarcaId", input.MarcaId);
    }

    private static void AgregarParametro(DbCommand command, string nombre, object valor)
    {
        var parametro = command.CreateParameter();
        parametro.ParameterName = nombre;
        parametro.Value = valor;
        command.Parameters.Add(parametro);
    }
}

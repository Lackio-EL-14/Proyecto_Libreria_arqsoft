using System.Data;
using Libreria.Web.Data;
using Libreria.Web.Pages.Productos.Models;

namespace Libreria.Web.Pages.Productos.Repositories;

public class ProductoRepository : IProductoRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ProductoRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public List<ProductoListItem> ObtenerProductos(
        string? busqueda,
        int? categoriaId,
        int? marcaId)
    {
        var productos = new List<ProductoListItem>();

        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT
                p.ProductoId,
                p.Nombre,
                p.Stock,
                p.PrecioVenta,
                p.CostoAdquisicionActual,
                c.Nombre AS Categoria,
                m.Nombre AS Marca
            FROM Producto p
            INNER JOIN Categoria c
                ON p.CategoriaId = c.CategoriaId
            INNER JOIN Marca m
                ON p.MarcaId = m.MarcaId
            WHERE p.Estado = 1
              AND (@Busqueda IS NULL
                   OR p.Nombre LIKE '%' + @Busqueda + '%'
                   OR p.DescripcionEspecifica LIKE '%' + @Busqueda + '%')
              AND (@CategoriaId IS NULL OR p.CategoriaId = @CategoriaId)
              AND (@MarcaId IS NULL OR p.MarcaId = @MarcaId)
            ORDER BY p.Nombre";

        AgregarParametro(
            command,
            "@Busqueda",
            string.IsNullOrWhiteSpace(busqueda)
                ? DBNull.Value
                : busqueda);

        AgregarParametro(
            command,
            "@CategoriaId",
            categoriaId.HasValue
                ? categoriaId.Value
                : DBNull.Value);

        AgregarParametro(
            command,
            "@MarcaId",
            marcaId.HasValue
                ? marcaId.Value
                : DBNull.Value);

        if (connection.State != ConnectionState.Open)
        {
            connection.Open();
        }

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            productos.Add(new ProductoListItem(
                Convert.ToInt32(reader["ProductoId"]),
                Convert.ToString(reader["Nombre"]) ?? string.Empty,
                Convert.ToInt32(reader["Stock"]),
                Convert.ToDecimal(reader["PrecioVenta"]),
                Convert.ToDecimal(reader["CostoAdquisicionActual"]),
                Convert.ToString(reader["Categoria"]) ?? string.Empty,
                Convert.ToString(reader["Marca"]) ?? string.Empty
            ));
        }

        return productos;
    }

    public ProductoDetalle? ObtenerProductoPorId(int productoId)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT
                p.ProductoId,
                p.Nombre,
                p.DescripcionEspecifica,
                p.FechaVencimiento,
                p.Stock,
                p.PrecioVenta,
                p.CostoAdquisicionActual,
                p.CategoriaId,
                c.Nombre AS Categoria,
                p.MarcaId,
                m.Nombre AS Marca
            FROM Producto p
            INNER JOIN Categoria c
                ON p.CategoriaId = c.CategoriaId
            INNER JOIN Marca m
                ON p.MarcaId = m.MarcaId
            WHERE p.ProductoId = @ProductoId
              AND p.Estado = 1";

        AgregarParametro(command, "@ProductoId", productoId);

        if (connection.State != ConnectionState.Open)
        {
            connection.Open();
        }

        using var reader = command.ExecuteReader();

        if (!reader.Read())
        {
            return null;
        }

        return new ProductoDetalle(
            Convert.ToInt32(reader["ProductoId"]),
            Convert.ToString(reader["Nombre"]) ?? string.Empty,
            reader["DescripcionEspecifica"] == DBNull.Value
                ? null
                : Convert.ToString(reader["DescripcionEspecifica"]),
            reader["FechaVencimiento"] == DBNull.Value
                ? null
                : Convert.ToDateTime(reader["FechaVencimiento"]),
            Convert.ToInt32(reader["Stock"]),
            Convert.ToDecimal(reader["PrecioVenta"]),
            Convert.ToDecimal(reader["CostoAdquisicionActual"]),
            Convert.ToInt32(reader["CategoriaId"]),
            Convert.ToString(reader["Categoria"]) ?? string.Empty,
            Convert.ToInt32(reader["MarcaId"]),
            Convert.ToString(reader["Marca"]) ?? string.Empty);
    }

    public List<CategoriaFiltroItem> ObtenerCategoriasActivas()
    {
        var categorias = new List<CategoriaFiltroItem>();

        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT CategoriaId, Nombre
            FROM Categoria
            WHERE Estado = 1
            ORDER BY Nombre";

        if (connection.State != ConnectionState.Open)
        {
            connection.Open();
        }

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            categorias.Add(new CategoriaFiltroItem(
                Convert.ToInt32(reader["CategoriaId"]),
                Convert.ToString(reader["Nombre"]) ?? string.Empty
            ));
        }

        return categorias;
    }

    public List<MarcaFiltroItem> ObtenerMarcasActivas()
    {
        var marcas = new List<MarcaFiltroItem>();

        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT MarcaId, Nombre
            FROM Marca
            WHERE Estado = 1
            ORDER BY Nombre";

        if (connection.State != ConnectionState.Open)
        {
            connection.Open();
        }

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            marcas.Add(new MarcaFiltroItem(
                Convert.ToInt32(reader["MarcaId"]),
                Convert.ToString(reader["Nombre"]) ?? string.Empty
            ));
        }

        return marcas;
    }


    public List<CategoriaOption> ObtenerCategoriasParaFormulario()
    {
        var categorias = new List<CategoriaOption>();

        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
        SELECT CategoriaId, Nombre
        FROM Categoria
        WHERE Estado = 1
        ORDER BY Nombre";

        if (connection.State != ConnectionState.Open)
        {
            connection.Open();
        }

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            categorias.Add(new CategoriaOption(
                Convert.ToInt32(reader["CategoriaId"]),
                Convert.ToString(reader["Nombre"]) ?? string.Empty
            ));
        }

        return categorias;
    }

    public List<MarcaOption> ObtenerMarcasParaFormulario()
    {
        var marcas = new List<MarcaOption>();

        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
        SELECT MarcaId, Nombre
        FROM Marca
        WHERE Estado = 1
        ORDER BY Nombre";

        if (connection.State != ConnectionState.Open)
        {
            connection.Open();
        }

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            marcas.Add(new MarcaOption(
                Convert.ToInt32(reader["MarcaId"]),
                Convert.ToString(reader["Nombre"]) ?? string.Empty
            ));
        }

        return marcas;
    }

    public bool ExisteCategoriaActiva(int categoriaId)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
        SELECT COUNT(1)
        FROM Categoria
        WHERE CategoriaId = @CategoriaId
          AND Estado = 1";

        AgregarParametro(command, "@CategoriaId", categoriaId);

        if (connection.State != ConnectionState.Open)
        {
            connection.Open();
        }

        var resultado = command.ExecuteScalar();

        return resultado != null
            && resultado != DBNull.Value
            && Convert.ToInt32(resultado) > 0;
    }

    public bool ExisteMarcaActiva(int marcaId)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
        SELECT COUNT(1)
        FROM Marca
        WHERE MarcaId = @MarcaId
          AND Estado = 1";

        AgregarParametro(command, "@MarcaId", marcaId);

        if (connection.State != ConnectionState.Open)
        {
            connection.Open();
        }

        var resultado = command.ExecuteScalar();

        return resultado != null
            && resultado != DBNull.Value
            && Convert.ToInt32(resultado) > 0;
    }


    public bool ActualizarProducto(int productoId, ProductoInput input)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
        UPDATE Producto
        SET Nombre = @Nombre,
            DescripcionEspecifica = @DescripcionEspecifica,
            FechaVencimiento = @FechaVencimiento,
            Stock = @Stock,
            PrecioVenta = @PrecioVenta,
            CategoriaId = @CategoriaId,
            MarcaId = @MarcaId,
            FechaModificacion = SYSDATETIME()
        WHERE ProductoId = @ProductoId
          AND Estado = 1";

        AgregarParametro(command, "@ProductoId", productoId);
        AgregarParametro(command, "@Nombre", input.Nombre);
        AgregarParametro(
            command,
            "@DescripcionEspecifica",
            string.IsNullOrWhiteSpace(input.DescripcionEspecifica)
                ? DBNull.Value
                : input.DescripcionEspecifica);
        AgregarParametro(
            command,
            "@FechaVencimiento",
            input.FechaVencimiento.HasValue
                ? input.FechaVencimiento.Value
                : DBNull.Value);
        AgregarParametro(command, "@Stock", input.Stock);
        AgregarParametro(command, "@PrecioVenta", input.PrecioVenta);
        AgregarParametro(command, "@CategoriaId", input.CategoriaId);
        AgregarParametro(command, "@MarcaId", input.MarcaId);

        if (connection.State != ConnectionState.Open)
        {
            connection.Open();
        }

        return command.ExecuteNonQuery() == 1;
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

        if (connection.State != ConnectionState.Open)
        {
            connection.Open();
        }

        return command.ExecuteNonQuery() == 1;
    }


    public int CrearProducto(
    ProductoInput input,
    IDbConnection connection,
    IDbTransaction transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;

        command.CommandText = @"
        INSERT INTO Producto
        (
            Nombre,
            DescripcionEspecifica,
            FechaVencimiento,
            Stock,
            PrecioVenta,
            CostoAdquisicionActual,
            CategoriaId,
            MarcaId,
            Estado
        )
        VALUES
        (
            @Nombre,
            @DescripcionEspecifica,
            @FechaVencimiento,
            @Stock,
            @PrecioVenta,
            @CostoAdquisicionActual,
            @CategoriaId,
            @MarcaId,
            1
        );

        SELECT CAST(SCOPE_IDENTITY() AS INT);";

        AgregarParametro(command, "@Nombre", input.Nombre);
        AgregarParametro(
            command,
            "@DescripcionEspecifica",
            string.IsNullOrWhiteSpace(input.DescripcionEspecifica)
                ? DBNull.Value
                : input.DescripcionEspecifica);

        AgregarParametro(
            command,
            "@FechaVencimiento",
            input.FechaVencimiento.HasValue
                ? input.FechaVencimiento.Value
                : DBNull.Value);

        AgregarParametro(command, "@Stock", input.Stock);
        AgregarParametro(command, "@PrecioVenta", input.PrecioVenta);
        AgregarParametro(command, "@CostoAdquisicionActual", input.CostoAdquisicion);
        AgregarParametro(command, "@CategoriaId", input.CategoriaId);
        AgregarParametro(command, "@MarcaId", input.MarcaId);

        var resultado = command.ExecuteScalar();

        if (resultado is null || resultado == DBNull.Value)
        {
            throw new InvalidOperationException(
                "No se pudo obtener el identificador del producto registrado.");
        }

        return Convert.ToInt32(resultado);
    }




    private static void AgregarParametro(
        IDbCommand command,
        string nombre,
        object valor)
    {
        var parametro = command.CreateParameter();
        parametro.ParameterName = nombre;
        parametro.Value = valor;
        command.Parameters.Add(parametro);
    }
}
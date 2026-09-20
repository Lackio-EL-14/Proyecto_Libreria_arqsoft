using System.Data;
using System.Data.Common;
using Libreria.Web.Domain.Entities;

namespace Libreria.Web.Data.Repositories;

public class MarcaRepository : ICrudRepository<Marca>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MarcaRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Marca>> ObtenerActivasAsync(
        string? busqueda = null)
    {
        var marcas = new List<Marca>();

        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT MarcaId,
                   PublicId,
                   Nombre,
                   Descripcion,
                   PaisOrigen,
                   SitioWeb,
                   Estado
            FROM Marca
            WHERE Estado = 1
              AND (@Busqueda IS NULL
                   OR Nombre LIKE '%' + @Busqueda + '%')
            ORDER BY Nombre";

        AgregarParametro(
            command,
            "@Busqueda",
            string.IsNullOrWhiteSpace(busqueda)
                ? DBNull.Value
                : busqueda.Trim(),
            DbType.String,
            100);

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            marcas.Add(MapearMarca(reader));
        }

        return marcas;
    }

    public async Task<Marca?> ObtenerPorPublicIdAsync(
        Guid publicId,
        bool estadoEsperado)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT MarcaId,
                   PublicId,
                   Nombre,
                   Descripcion,
                   PaisOrigen,
                   SitioWeb,
                   Estado
            FROM Marca
            WHERE PublicId = @PublicId
              AND Estado = @Estado";

        AgregarParametro(
            command,
            "@PublicId",
            publicId,
            DbType.Guid);

        AgregarParametro(
            command,
            "@Estado",
            estadoEsperado,
            DbType.Boolean);

        using var reader = await command.ExecuteReaderAsync();

        return await reader.ReadAsync()
            ? MapearMarca(reader)
            : null;
    }

    public async Task CrearAsync(Marca marca)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            INSERT INTO Marca
                (Nombre, Descripcion, PaisOrigen, SitioWeb, Estado)
            VALUES
                (@Nombre, @Descripcion, @PaisOrigen, @SitioWeb, 1)";

        AgregarDatosMarca(command, marca);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<bool> ActualizarAsync(Marca marca)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            UPDATE Marca
            SET Nombre = @Nombre,
                Descripcion = @Descripcion,
                PaisOrigen = @PaisOrigen,
                SitioWeb = @SitioWeb,
                FechaModificacion = SYSDATETIME()
            WHERE PublicId = @PublicId
              AND Estado = 1";

        AgregarDatosMarca(command, marca);

        AgregarParametro(
            command,
            "@PublicId",
            marca.PublicId,
            DbType.Guid);

        return await command.ExecuteNonQueryAsync() == 1;
    }

    public async Task<bool> CambiarEstadoAsync(
        Guid publicId,
        bool nuevoEstado)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            UPDATE Marca
            SET Estado = @NuevoEstado,
                FechaModificacion = SYSDATETIME()
            WHERE PublicId = @PublicId";

        AgregarParametro(
            command,
            "@NuevoEstado",
            nuevoEstado,
            DbType.Boolean);

        AgregarParametro(
            command,
            "@PublicId",
            publicId,
            DbType.Guid);

        return await command.ExecuteNonQueryAsync() == 1;
    }

    public async Task<bool> ExisteValorAsync(
        string columna,
        string valor,
        Guid? excluirPublicId = null)
    {
        if (columna != "Nombre")
        {
            throw new ArgumentException(
                "La columna indicada no es válida.",
                nameof(columna));
        }

        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT COUNT(1)
            FROM Marca
            WHERE Nombre = @Valor
              AND (@ExcluirPublicId IS NULL
                   OR PublicId <> @ExcluirPublicId)";

        AgregarParametro(
            command,
            "@Valor",
            valor,
            DbType.String,
            100);

        AgregarParametro(
            command,
            "@ExcluirPublicId",
            excluirPublicId.HasValue
                ? excluirPublicId.Value
                : DBNull.Value,
            DbType.Guid);

        return Convert.ToInt32(
            await command.ExecuteScalarAsync()) > 0;
    }

    public async Task<bool> TieneRelacionesAsync(Guid publicId)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT COUNT(1)
            FROM Producto p
            INNER JOIN Marca m
                ON p.MarcaId = m.MarcaId
            WHERE m.PublicId = @PublicId
              AND p.Estado = 1";

        AgregarParametro(
            command,
            "@PublicId",
            publicId,
            DbType.Guid);

        return Convert.ToInt32(
            await command.ExecuteScalarAsync()) > 0;
    }

    private static Marca MapearMarca(DbDataReader reader)
    {
        return new Marca
        {
            MarcaId = reader.GetInt32(0),
            PublicId = reader.GetGuid(1),
            Nombre = reader.GetString(2),
            Descripcion = ObtenerTextoOpcional(reader, 3),
            PaisOrigen = reader.GetString(4),
            SitioWeb = ObtenerTextoOpcional(reader, 5),
            Estado = reader.GetBoolean(6)
        };
    }

    private static string? ObtenerTextoOpcional(
        DbDataReader reader,
        int indice)
    {
        return reader.IsDBNull(indice)
            ? null
            : reader.GetString(indice);
    }

    private static void AgregarDatosMarca(
        DbCommand command,
        Marca marca)
    {
        AgregarParametro(
            command,
            "@Nombre",
            marca.Nombre,
            DbType.String,
            100);

        AgregarParametro(
            command,
            "@Descripcion",
            marca.Descripcion ?? (object)DBNull.Value,
            DbType.String,
            255);

        AgregarParametro(
            command,
            "@PaisOrigen",
            marca.PaisOrigen,
            DbType.String,
            100);

        AgregarParametro(
            command,
            "@SitioWeb",
            marca.SitioWeb ?? (object)DBNull.Value,
            DbType.String,
            200);
    }

    private static void AgregarParametro(
        DbCommand command,
        string nombre,
        object valor,
        DbType tipo,
        int? tamanio = null)
    {
        var parametro = command.CreateParameter();

        parametro.ParameterName = nombre;
        parametro.Value = valor;
        parametro.DbType = tipo;

        if (tamanio.HasValue)
        {
            parametro.Size = tamanio.Value;
        }

        command.Parameters.Add(parametro);
    }
}
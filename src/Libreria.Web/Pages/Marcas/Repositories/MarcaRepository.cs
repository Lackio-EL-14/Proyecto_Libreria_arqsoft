using System.Data;
using System.Data.Common;
using Libreria.Web.Data;
using Libreria.Web.Pages.Marcas.Models;

namespace Libreria.Web.Pages.Marcas.Repositories;

public class MarcaRepository : IMarcaRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MarcaRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public IReadOnlyList<MarcaListItem> ObtenerActivas(string? nombre)
    {
        var marcas = new List<MarcaListItem>();
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT MarcaId, Nombre, Descripcion, PaisOrigen, SitioWeb
            FROM Marca
            WHERE Estado = 1
              AND (@Nombre IS NULL OR Nombre LIKE '%' + @Nombre + '%')
            ORDER BY Nombre";
        AgregarParametro(command, "@Nombre",
            string.IsNullOrWhiteSpace(nombre) ? DBNull.Value : nombre.Trim(), DbType.String, 100);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            marcas.Add(new MarcaListItem(
                reader.GetInt32(0),
                reader.GetString(1),
                ObtenerTextoOpcional(reader, 2),
                reader.GetString(3),
                ObtenerTextoOpcional(reader, 4)));
        }

        return marcas;
    }

    public void Crear(Marca marca)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Marca (Nombre, Descripcion, PaisOrigen, SitioWeb, Estado)
            VALUES (@Nombre, @Descripcion, @PaisOrigen, @SitioWeb, 1)";
        AgregarDatosMarca(command, marca);
        command.ExecuteNonQuery();
    }

    public Marca? ObtenerActivaPorId(int marcaId)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT MarcaId, Nombre, Descripcion, PaisOrigen, SitioWeb, Estado
            FROM Marca
            WHERE MarcaId = @MarcaId
              AND Estado = 1";
        AgregarParametro(command, "@MarcaId", marcaId, DbType.Int32);
        using var reader = command.ExecuteReader();
        return reader.Read() ? MapearMarca(reader) : null;
    }

    public bool Actualizar(Marca marca)
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
            WHERE MarcaId = @MarcaId
              AND Estado = 1";
        AgregarDatosMarca(command, marca);
        AgregarParametro(command, "@MarcaId", marca.MarcaId, DbType.Int32);
        return command.ExecuteNonQuery() == 1;
    }

    public MarcaBajaView? ObtenerActivaParaBaja(int marcaId)
    {
        var marca = ObtenerActivaPorId(marcaId);
        if (marca is null)
        {
            return null;
        }

        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT COUNT(1)
            FROM Producto
            WHERE MarcaId = @MarcaId
              AND Estado = 1";
        AgregarParametro(command, "@MarcaId", marcaId, DbType.Int32);

        return new MarcaBajaView
        {
            MarcaId = marca.MarcaId,
            Nombre = marca.Nombre,
            Descripcion = marca.Descripcion,
            PaisOrigen = marca.PaisOrigen,
            SitioWeb = marca.SitioWeb,
            ProductosActivos = Convert.ToInt32(command.ExecuteScalar())
        };
    }

    public bool DarDeBaja(int marcaId)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE Marca
            SET Estado = 0,
                FechaModificacion = SYSDATETIME()
            WHERE MarcaId = @MarcaId
              AND Estado = 1";
        AgregarParametro(command, "@MarcaId", marcaId, DbType.Int32);
        return command.ExecuteNonQuery() == 1;
    }

    public bool ExisteNombre(string nombre, int? excluirId)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT COUNT(1)
            FROM Marca
            WHERE Nombre = @Nombre
              AND (@ExcluirId IS NULL OR MarcaId <> @ExcluirId)";
        AgregarParametro(command, "@Nombre", nombre, DbType.String, 100);
        AgregarParametro(command, "@ExcluirId", excluirId ?? (object)DBNull.Value, DbType.Int32);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    private static Marca MapearMarca(DbDataReader reader)
    {
        return new Marca
        {
            MarcaId = reader.GetInt32(0),
            Nombre = reader.GetString(1),
            Descripcion = ObtenerTextoOpcional(reader, 2),
            PaisOrigen = reader.GetString(3),
            SitioWeb = ObtenerTextoOpcional(reader, 4),
            Estado = reader.GetBoolean(5)
        };
    }

    private static string? ObtenerTextoOpcional(DbDataReader reader, int indice)
    {
        return reader.IsDBNull(indice) ? null : reader.GetString(indice);
    }

    private static void AgregarDatosMarca(DbCommand command, Marca marca)
    {
        AgregarParametro(command, "@Nombre", marca.Nombre, DbType.String, 100);
        AgregarParametro(command, "@Descripcion", marca.Descripcion ?? (object)DBNull.Value, DbType.String, 255);
        AgregarParametro(command, "@PaisOrigen", marca.PaisOrigen, DbType.String, 100);
        AgregarParametro(command, "@SitioWeb", marca.SitioWeb ?? (object)DBNull.Value, DbType.String, 200);
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

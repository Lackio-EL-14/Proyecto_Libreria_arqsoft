using Libreria.Application.Domain;
using Libreria.Application.Ports.Secondary;
using System.Data;
using System.Data.Common;

namespace Libreria.Infrastructure;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UsuarioRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Usuario?> ObtenerActivoPorNombreUsuarioAsync(string nombreUsuario)
    {
        await using var connection = await CrearConexionAbiertaAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT u.UsuarioId, u.PublicId, u.NombreUsuario, u.NombreCompleto,
                   u.PasswordHash, u.RolId, u.Estado, u.FechaCreacion,
                   u.FechaModificacion,
                   r.Nombre AS RolNombre, r.Descripcion AS RolDescripcion,
                   r.Estado AS RolEstado
            FROM Usuario u
            INNER JOIN Rol r ON u.RolId = r.RolId
            WHERE u.NombreUsuario = @NombreUsuario
              AND u.Estado = 1
              AND r.Estado = 1";

        AgregarParametro(command, "@NombreUsuario", nombreUsuario.Trim(), DbType.String, 50);

        await using var reader = await command.ExecuteReaderAsync();

        return await reader.ReadAsync()
            ? MapearUsuario(reader)
            : null;
    }

    public async Task<string?> ObtenerNombreCompletoPorIdAsync(int usuarioId)
    {
        await using var connection = await CrearConexionAbiertaAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT NombreCompleto
            FROM Usuario
            WHERE UsuarioId = @UsuarioId";

        AgregarParametro(command, "@UsuarioId", usuarioId, DbType.Int32);

        var resultado = await command.ExecuteScalarAsync();

        return resultado is null || resultado == DBNull.Value
            ? null
            : Convert.ToString(resultado);
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

    private static Usuario MapearUsuario(DbDataReader reader)
    {
        var rolId = reader.GetInt32(reader.GetOrdinal("RolId"));

        return new Usuario
        {
            UsuarioId = reader.GetInt32(reader.GetOrdinal("UsuarioId")),
            PublicId = reader.GetGuid(reader.GetOrdinal("PublicId")),
            NombreUsuario = reader.GetString(reader.GetOrdinal("NombreUsuario")),
            NombreCompleto = reader.GetString(reader.GetOrdinal("NombreCompleto")),
            PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash")),
            RolId = rolId,
            Estado = reader.GetBoolean(reader.GetOrdinal("Estado")),
            FechaCreacion = reader.GetDateTime(reader.GetOrdinal("FechaCreacion")),
            FechaModificacion = reader.IsDBNull(reader.GetOrdinal("FechaModificacion"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("FechaModificacion")),
            Rol = new Rol
            {
                RolId = rolId,
                Nombre = reader.GetString(reader.GetOrdinal("RolNombre")),
                Descripcion = reader.IsDBNull(reader.GetOrdinal("RolDescripcion"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("RolDescripcion")),
                Estado = reader.GetBoolean(reader.GetOrdinal("RolEstado"))
            }
        };
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

using Libreria.Domain;
using Libreria.Application.Models;
using Libreria.Application.Ports.Secondary;
using System.Data;
using System.Data.Common;

namespace Libreria.Infrastructure;

public class UsuarioRepository : IUsuarioRepository, IUsuarioSeguridadRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UsuarioRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<bool> TieneAccesoActivoAsync(int usuarioId)
    {
        await using var connection = await CrearConexionAbiertaAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT COUNT(1) FROM dbo.Usuario u
            INNER JOIN dbo.Rol r ON r.RolId = u.RolId
            WHERE u.UsuarioId = @UsuarioId AND u.Estado = @Activo AND r.Estado = 1";
        AgregarParametro(command, "@UsuarioId", usuarioId, DbType.Int32);
        AgregarParametro(command, "@Activo", EstadosUsuario.Activo, DbType.String, 20);
        return Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
    }

    public async Task<IReadOnlyList<UsuarioBajaDatos>> BloquearParaBajaAsync(
        IDbConnection connection, IDbTransaction transaction)
    {
        var (dbConnection, dbTransaction) = ObtenerConexionYTransaccion(connection, transaction);
        await using var command = dbConnection.CreateCommand();
        command.Transaction = dbTransaction;
        // Baja administrativa poco frecuente: serializar en SQL entre todas las replicas.
        command.CommandText = @"
            SELECT u.UsuarioId, u.PublicId, u.Estado, r.Nombre AS Rol, r.Estado AS RolActivo
            FROM dbo.Usuario u WITH (TABLOCKX, HOLDLOCK)
            INNER JOIN dbo.Rol r WITH (HOLDLOCK) ON r.RolId = u.RolId";
        var usuarios = new List<UsuarioBajaDatos>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            usuarios.Add(new UsuarioBajaDatos(
                reader.GetInt32(0), reader.GetGuid(1), reader.GetString(2),
                reader.GetString(3), reader.GetBoolean(4)));
        }
        return usuarios;
    }

    public async Task<bool> DesactivarAsync(int usuarioId, int usuarioModificacionId,
        IDbConnection connection, IDbTransaction transaction)
    {
        var (dbConnection, dbTransaction) = ObtenerConexionYTransaccion(connection, transaction);
        await using var command = dbConnection.CreateCommand();
        command.Transaction = dbTransaction;
        command.CommandText = @"
            UPDATE dbo.Usuario SET Estado = @Inactivo,
                UsuarioModificacionId = @ActorId, FechaModificacion = SYSDATETIME()
            WHERE UsuarioId = @UsuarioId AND Estado <> @Inactivo";
        AgregarParametro(command, "@Inactivo", EstadosUsuario.Inactivo, DbType.String, 20);
        AgregarParametro(command, "@ActorId", usuarioModificacionId, DbType.Int32);
        AgregarParametro(command, "@UsuarioId", usuarioId, DbType.Int32);
        return await command.ExecuteNonQueryAsync() == 1;
    }

    public async Task<IReadOnlyList<UsuarioListItemModel>> ObtenerListadoAsync(string? estado = null)
    {
        await using var connection = await CrearConexionAbiertaAsync();
        await using var command = connection.CreateCommand();

        // Proyeccion de lectura: no cargar credenciales ni tokens para el listado.
        command.CommandText = @"
            SELECT u.PublicId, u.NombreCompleto, u.NombreUsuario, u.Correo,
                   r.Nombre AS Rol, u.Estado, u.FechaCreacion,
                   creador.NombreCompleto AS CreadoPor
            FROM dbo.Usuario u
            INNER JOIN dbo.Rol r ON r.RolId = u.RolId
            LEFT JOIN dbo.Usuario creador ON creador.UsuarioId = u.UsuarioCreacionId
            WHERE (@Estado IS NULL OR u.Estado = @Estado)
            ORDER BY u.FechaCreacion DESC, u.NombreUsuario ASC";

        AgregarParametro(command, "@Estado", estado is null ? DBNull.Value : estado, DbType.String, 20);

        var usuarios = new List<UsuarioListItemModel>();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            usuarios.Add(new UsuarioListItemModel(
                reader.GetGuid(reader.GetOrdinal("PublicId")),
                reader.GetString(reader.GetOrdinal("NombreCompleto")),
                reader.GetString(reader.GetOrdinal("NombreUsuario")),
                reader.GetString(reader.GetOrdinal("Correo")),
                reader.GetString(reader.GetOrdinal("Rol")),
                reader.GetString(reader.GetOrdinal("Estado")),
                reader.GetDateTime(reader.GetOrdinal("FechaCreacion")),
                reader.IsDBNull(reader.GetOrdinal("CreadoPor"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("CreadoPor"))));
        }

        return usuarios;
    }

    public async Task<Usuario?> ObtenerActivoPorNombreUsuarioAsync(string nombreUsuario)
    {
        await using var connection = await CrearConexionAbiertaAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT u.UsuarioId, u.PublicId,
                   u.Nombre, u.Apellido, u.Correo,
                   u.NombreUsuario, u.NombreCompleto,
                   u.PasswordHash, u.RolId, u.Estado,
                   u.TokenActivacionHash,
                   u.TokenActivacionExpiracion,
                   u.TokenActivacionFechaUso,
                   u.FechaCreacion, u.FechaModificacion,
                   u.UsuarioCreacionId, u.UsuarioModificacionId,
                   r.Nombre AS RolNombre,
                   r.Descripcion AS RolDescripcion,
                   r.Estado AS RolEstado
            FROM Usuario u
            INNER JOIN Rol r ON u.RolId = r.RolId
            WHERE u.NombreUsuario = @NombreUsuario
              AND u.Estado = @EstadoActivo
              AND r.Estado = 1";

        AgregarParametro(command, "@NombreUsuario", nombreUsuario.Trim(), DbType.String, 50);
        AgregarParametro(command, "@EstadoActivo", EstadosUsuario.Activo, DbType.String, 20);

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

    public async Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario)
    {
        await using var connection = await CrearConexionAbiertaAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = @"
        SELECT COUNT(1)
        FROM Usuario
        WHERE NombreUsuario = @NombreUsuario";

        AgregarParametro(
            command,
            "@NombreUsuario",
            nombreUsuario.Trim(),
            DbType.String,
            50);

        var resultado = await command.ExecuteScalarAsync();

        return Convert.ToInt32(resultado) > 0;
    }

    public async Task<bool> ExisteCorreoAsync(string correo)
    {
        await using var connection = await CrearConexionAbiertaAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = @"
        SELECT COUNT(1)
        FROM Usuario
        WHERE Correo = @Correo";

        AgregarParametro(
            command,
            "@Correo",
            correo.Trim(),
            DbType.String,
            254);

        var resultado = await command.ExecuteScalarAsync();

        return Convert.ToInt32(resultado) > 0;
    }
    public async Task<int> InsertarAsync(Usuario usuario)
    {
        await using var connection = await CrearConexionAbiertaAsync();

        return await InsertarInternoAsync(
            usuario,
            connection,
            transaction: null);
    }

    public async Task<int> InsertarAsync(
        Usuario usuario,
        IDbConnection connection,
        IDbTransaction transaction)
    {
        var (dbConnection, dbTransaction) =
            ObtenerConexionYTransaccion(connection, transaction);

        return await InsertarInternoAsync(
            usuario,
            dbConnection,
            dbTransaction);
    }

    private static async Task<int> InsertarInternoAsync(
        Usuario usuario,
        DbConnection connection,
        DbTransaction? transaction)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;

        command.CommandText = @"
        INSERT INTO dbo.Usuario
        (
            Nombre,
            Apellido,
            Correo,
            NombreUsuario,
            NombreCompleto,
            PasswordHash,
            RolId,
            Estado,
            TokenActivacionHash,
            TokenActivacionExpiracion,
            TokenActivacionFechaUso,
            UsuarioCreacionId,
            UsuarioModificacionId
        )
        OUTPUT INSERTED.UsuarioId
        VALUES
        (
            @Nombre,
            @Apellido,
            @Correo,
            @NombreUsuario,
            @NombreCompleto,
            @PasswordHash,
            @RolId,
            @Estado,
            @TokenActivacionHash,
            @TokenActivacionExpiracion,
            @TokenActivacionFechaUso,
            @UsuarioCreacionId,
            @UsuarioModificacionId
        );";

        var nombreCompleto =
            $"{usuario.Nombre.Trim()} {usuario.Apellido.Trim()}".Trim();

        AgregarParametro(
            command,
            "@Nombre",
            usuario.Nombre.Trim(),
            DbType.String,
            100);

        AgregarParametro(
            command,
            "@Apellido",
            usuario.Apellido.Trim(),
            DbType.String,
            100);

        AgregarParametro(
            command,
            "@Correo",
            usuario.Correo.Trim(),
            DbType.String,
            254);

        AgregarParametro(
            command,
            "@NombreUsuario",
            usuario.NombreUsuario.Trim(),
            DbType.String,
            50);

        AgregarParametro(
            command,
            "@NombreCompleto",
            nombreCompleto,
            DbType.String,
            150);

        AgregarParametro(
            command,
            "@PasswordHash",
            string.IsNullOrWhiteSpace(usuario.PasswordHash)
                ? DBNull.Value
                : usuario.PasswordHash,
            DbType.String,
            500);

        AgregarParametro(
            command,
            "@RolId",
            usuario.RolId,
            DbType.Int32);

        AgregarParametro(
            command,
            "@Estado",
            usuario.Estado,
            DbType.String,
            20);

        AgregarParametro(
            command,
            "@TokenActivacionHash",
            usuario.TokenActivacionHash is null
                ? DBNull.Value
                : usuario.TokenActivacionHash,
            DbType.String,
            200);

        AgregarParametro(
            command,
            "@TokenActivacionExpiracion",
            usuario.TokenActivacionExpiracion.HasValue
                ? usuario.TokenActivacionExpiracion.Value
                : DBNull.Value,
            DbType.DateTime2);

        AgregarParametro(
            command,
            "@TokenActivacionFechaUso",
            usuario.TokenActivacionFechaUso.HasValue
                ? usuario.TokenActivacionFechaUso.Value
                : DBNull.Value,
            DbType.DateTime2);

        AgregarParametro(
            command,
            "@UsuarioCreacionId",
            usuario.UsuarioCreacionId.HasValue
                ? usuario.UsuarioCreacionId.Value
                : DBNull.Value,
            DbType.Int32);

        AgregarParametro(
            command,
            "@UsuarioModificacionId",
            usuario.UsuarioModificacionId.HasValue
                ? usuario.UsuarioModificacionId.Value
                : DBNull.Value,
            DbType.Int32);

        var resultado = await command.ExecuteScalarAsync();

        return Convert.ToInt32(resultado);
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

            Nombre = reader.GetString(reader.GetOrdinal("Nombre")),
            Apellido = reader.GetString(reader.GetOrdinal("Apellido")),
            Correo = reader.GetString(reader.GetOrdinal("Correo")),

            NombreUsuario = reader.GetString(reader.GetOrdinal("NombreUsuario")),
            NombreCompleto = reader.GetString(reader.GetOrdinal("NombreCompleto")),

            PasswordHash = reader.IsDBNull(reader.GetOrdinal("PasswordHash"))
                ? string.Empty
                : reader.GetString(reader.GetOrdinal("PasswordHash")),

            RolId = rolId,
            Estado = reader.GetString(reader.GetOrdinal("Estado")),

            TokenActivacionHash =
                reader.IsDBNull(reader.GetOrdinal("TokenActivacionHash"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("TokenActivacionHash")),

            TokenActivacionExpiracion =
                reader.IsDBNull(reader.GetOrdinal("TokenActivacionExpiracion"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("TokenActivacionExpiracion")),

            TokenActivacionFechaUso =
                reader.IsDBNull(reader.GetOrdinal("TokenActivacionFechaUso"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("TokenActivacionFechaUso")),

            FechaCreacion = reader.GetDateTime(reader.GetOrdinal("FechaCreacion")),

            FechaModificacion =
                reader.IsDBNull(reader.GetOrdinal("FechaModificacion"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("FechaModificacion")),

            UsuarioCreacionId =
                reader.IsDBNull(reader.GetOrdinal("UsuarioCreacionId"))
                    ? null
                    : reader.GetInt32(reader.GetOrdinal("UsuarioCreacionId")),

            UsuarioModificacionId =
                reader.IsDBNull(reader.GetOrdinal("UsuarioModificacionId"))
                    ? null
                    : reader.GetInt32(reader.GetOrdinal("UsuarioModificacionId")),

            Rol = new Rol
            {
                RolId = rolId,
                Nombre = reader.GetString(reader.GetOrdinal("RolNombre")),
                Descripcion =
                    reader.IsDBNull(reader.GetOrdinal("RolDescripcion"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("RolDescripcion")),
                Estado = reader.GetBoolean(reader.GetOrdinal("RolEstado"))
            }
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

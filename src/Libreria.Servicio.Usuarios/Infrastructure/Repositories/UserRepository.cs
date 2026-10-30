using System.Data;
using System.Data.Common;
using Libreria.Servicio.Usuarios.Application.Abstractions;
using Libreria.Servicio.Usuarios.Domain.Constants;
using Libreria.Servicio.Usuarios.Domain.Entities;
using Libreria.Servicio.Usuarios.Infrastructure.Connection;

namespace Libreria.Servicio.Usuarios.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public UserRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<User?> GetActiveByUsernameAsync(string username)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT
                u.UsuarioId,
                u.PublicId,
                u.Nombre,
                u.Apellido,
                u.Correo,
                u.NombreUsuario,
                u.NombreCompleto,
                u.PasswordHash,
                u.RolId,
                u.Estado,
                u.TokenActivacionHash,
                u.TokenActivacionExpiracion,
                u.TokenActivacionFechaUso,
                u.FechaCreacion,
                u.FechaModificacion,
                u.UsuarioCreacionId,
                u.UsuarioModificacionId,
                r.Nombre AS RolNombre,
                r.Descripcion AS RolDescripcion,
                r.Estado AS RolEstado
            FROM dbo.Usuario u
            INNER JOIN dbo.Rol r
                ON r.RolId = u.RolId
            WHERE u.NombreUsuario = @Username
              AND u.Estado = @ActiveStatus
              AND r.Estado = 1;
            """;

        AddParameter(
            command,
            "@Username",
            username.Trim(),
            DbType.String,
            50);

        AddParameter(
            command,
            "@ActiveStatus",
            UserStatuses.Active,
            DbType.String,
            20);

        await using var reader = await command.ExecuteReaderAsync();

        return await reader.ReadAsync()
            ? MapUser(reader)
            : null;
    }

    public async Task<bool> UpdatePasswordAsync(
        int userId,
        string previousHash,
        string newHash)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await using var command = connection.CreateCommand();

        command.CommandText = """
            UPDATE dbo.Usuario
            SET PasswordHash = @NewHash,
                UsuarioModificacionId = @UserId,
                FechaModificacion = SYSDATETIME()
            WHERE UsuarioId = @UserId
              AND Estado = @ActiveStatus
              AND PasswordHash COLLATE Latin1_General_100_BIN2 = @PreviousHash
              AND EXISTS
              (
                  SELECT 1
                  FROM dbo.Rol r
                  WHERE r.RolId = dbo.Usuario.RolId
                    AND r.Estado = 1
              );
            """;

        AddParameter(
            command,
            "@UserId",
            userId,
            DbType.Int32);

        AddParameter(
            command,
            "@ActiveStatus",
            UserStatuses.Active,
            DbType.String,
            20);

        AddParameter(
            command,
            "@PreviousHash",
            previousHash,
            DbType.String,
            500);

        AddParameter(
            command,
            "@NewHash",
            newHash,
            DbType.String,
            500);

        return await command.ExecuteNonQueryAsync() == 1;
    }

    public async Task<bool> IsSessionActiveAsync(int userId)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT COUNT(1)
            FROM dbo.Usuario u
            INNER JOIN dbo.Rol r
                ON r.RolId = u.RolId
            WHERE u.UsuarioId = @UserId
              AND u.Estado = @ActiveStatus
              AND r.Estado = 1;
            """;

        AddParameter(
            command,
            "@UserId",
            userId,
            DbType.Int32);

        AddParameter(
            command,
            "@ActiveStatus",
            UserStatuses.Active,
            DbType.String,
            20);

        return Convert.ToInt32(
            await command.ExecuteScalarAsync()) == 1;
    }

    private static User MapUser(DbDataReader reader)
    {
        var roleId = reader.GetInt32(
            reader.GetOrdinal("RolId"));

        return new User
        {
            Id = reader.GetInt32(
                reader.GetOrdinal("UsuarioId")),

            PublicId = reader.GetGuid(
                reader.GetOrdinal("PublicId")),

            FirstName = reader.GetString(
                reader.GetOrdinal("Nombre")),

            LastName = reader.GetString(
                reader.GetOrdinal("Apellido")),

            Email = reader.GetString(
                reader.GetOrdinal("Correo")),

            Username = reader.GetString(
                reader.GetOrdinal("NombreUsuario")),

            FullName = reader.GetString(
                reader.GetOrdinal("NombreCompleto")),

            PasswordHash =
                reader.IsDBNull(
                    reader.GetOrdinal("PasswordHash"))
                    ? string.Empty
                    : reader.GetString(
                        reader.GetOrdinal("PasswordHash")),

            RoleId = roleId,

            Status = reader.GetString(
                reader.GetOrdinal("Estado")),

            ActivationTokenHash =
                reader.IsDBNull(
                    reader.GetOrdinal("TokenActivacionHash"))
                    ? null
                    : reader.GetString(
                        reader.GetOrdinal("TokenActivacionHash")),

            ActivationTokenExpiresAt =
                reader.IsDBNull(
                    reader.GetOrdinal("TokenActivacionExpiracion"))
                    ? null
                    : reader.GetDateTime(
                        reader.GetOrdinal("TokenActivacionExpiracion")),

            ActivationTokenUsedAt =
                reader.IsDBNull(
                    reader.GetOrdinal("TokenActivacionFechaUso"))
                    ? null
                    : reader.GetDateTime(
                        reader.GetOrdinal("TokenActivacionFechaUso")),

            CreatedAt = reader.GetDateTime(
                reader.GetOrdinal("FechaCreacion")),

            ModifiedAt =
                reader.IsDBNull(
                    reader.GetOrdinal("FechaModificacion"))
                    ? null
                    : reader.GetDateTime(
                        reader.GetOrdinal("FechaModificacion")),

            CreatedByUserId =
                reader.IsDBNull(
                    reader.GetOrdinal("UsuarioCreacionId"))
                    ? null
                    : reader.GetInt32(
                        reader.GetOrdinal("UsuarioCreacionId")),

            ModifiedByUserId =
                reader.IsDBNull(
                    reader.GetOrdinal("UsuarioModificacionId"))
                    ? null
                    : reader.GetInt32(
                        reader.GetOrdinal("UsuarioModificacionId")),

            Role = new Role
            {
                Id = roleId,

                Name = reader.GetString(
                    reader.GetOrdinal("RolNombre")),

                Description =
                    reader.IsDBNull(
                        reader.GetOrdinal("RolDescripcion"))
                        ? null
                        : reader.GetString(
                            reader.GetOrdinal("RolDescripcion")),

                IsActive = reader.GetBoolean(
                    reader.GetOrdinal("RolEstado"))
            }
        };
    }

    private static void AddParameter(
        DbCommand command,
        string name,
        object value,
        DbType type,
        int? size = null)
    {
        var parameter = command.CreateParameter();

        parameter.ParameterName = name;
        parameter.Value = value;
        parameter.DbType = type;

        if (size.HasValue)
        {
            parameter.Size = size.Value;
        }

        command.Parameters.Add(parameter);
    }
}
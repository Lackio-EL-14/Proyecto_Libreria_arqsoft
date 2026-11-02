using System.Data;
using System.Data.Common;
using Libreria.Servicio.Usuarios.Application.Abstractions;
using Libreria.Servicio.Usuarios.Domain.Constants;
using Libreria.Servicio.Usuarios.Domain.Entities;
using Libreria.Servicio.Usuarios.Infrastructure.Connection;
using Libreria.Servicio.Usuarios.Application.DTOs.Users;

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

    public async Task<string?> GetActivePasswordHashAsync(int userId)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await using var command = connection.CreateCommand();

        command.CommandText = """
        SELECT u.PasswordHash
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

        var result = await command.ExecuteScalarAsync();

        return result is null || result == DBNull.Value
            ? null
            : Convert.ToString(result);
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

    public async Task<IReadOnlyDictionary<int, string>> GetFullNamesByIdsAsync(
    IReadOnlyCollection<int> userIds)
    {
        ArgumentNullException.ThrowIfNull(userIds);

        var ids = userIds
            .Where(id => id > 0)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<int, string>();
        }

        await using var connection =
            _connectionFactory.CreateConnection();

        await using var command =
            connection.CreateCommand();

        var parameterNames = new string[ids.Length];

        for (var index = 0; index < ids.Length; index++)
        {
            var parameterName = $"@UserId{index}";
            parameterNames[index] = parameterName;

            AddParameter(
                command,
                parameterName,
                ids[index],
                DbType.Int32);
        }

        command.CommandText = $"""
            SELECT
                UsuarioId,
                NombreCompleto
            FROM dbo.Usuario
            WHERE UsuarioId IN ({string.Join(", ", parameterNames)});
            """;

        var result = new Dictionary<int, string>();

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var userId = reader.GetInt32(
                reader.GetOrdinal("UsuarioId"));

            var fullName = reader.GetString(
                reader.GetOrdinal("NombreCompleto"));

            result[userId] = fullName;
        }

        return result;
    }


    public async Task<bool> IsActiveAdministratorAsync(int userId)
    {
        await using var connection =
            _connectionFactory.CreateConnection();

        await using var command =
            connection.CreateCommand();

        command.CommandText = """
            SELECT COUNT(1)
            FROM dbo.Usuario u
            INNER JOIN dbo.Rol r
                ON r.RolId = u.RolId
            WHERE u.UsuarioId = @UserId
            AND u.Estado = @ActiveStatus
            AND r.Nombre = @AdministratorRole
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

        AddParameter(
            command,
            "@AdministratorRole",
            SystemRoles.Administrator,
            DbType.String,
            50);

        return Convert.ToInt32(
            await command.ExecuteScalarAsync()) == 1;
    }

    public async Task<IReadOnlyList<UserListItemDto>> GetListAsync(
        string? status = null)
    {
        await using var connection =
            _connectionFactory.CreateConnection();

        await using var command =
            connection.CreateCommand();

        command.CommandText = """
            SELECT
                u.PublicId,
                u.NombreCompleto,
                u.NombreUsuario,
                u.Correo,
                r.Nombre AS Rol,
                u.Estado
            FROM dbo.Usuario u
            INNER JOIN dbo.Rol r
                ON r.RolId = u.RolId
            WHERE (@Status IS NULL OR u.Estado = @Status)
            ORDER BY u.FechaCreacion DESC, u.NombreUsuario ASC;
            """;

        AddParameter(
            command,
            "@Status",
            status is null ? DBNull.Value : status,
            DbType.String,
            20);

        var users = new List<UserListItemDto>();

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            users.Add(
                new UserListItemDto(
                    reader.GetGuid(
                        reader.GetOrdinal("PublicId")),
                    reader.GetString(
                        reader.GetOrdinal("NombreCompleto")),
                    reader.GetString(
                        reader.GetOrdinal("NombreUsuario")),
                    reader.GetString(
                        reader.GetOrdinal("Correo")),
                    reader.GetString(
                        reader.GetOrdinal("Rol")),
                    reader.GetString(
                        reader.GetOrdinal("Estado"))));
        }

        return users;
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        await using var connection =
            _connectionFactory.CreateConnection();

        await using var command =
            connection.CreateCommand();

        command.CommandText = """
            SELECT COUNT(1)
            FROM dbo.Usuario
            WHERE Correo = @Email;
            """;

        AddParameter(
            command,
            "@Email",
            email.Trim(),
            DbType.String,
            254);

        return Convert.ToInt32(
            await command.ExecuteScalarAsync()) > 0;
    }

    public async Task<int?> GetActiveRoleIdByNameAsync(string roleName)
    {
        await using var connection =
            _connectionFactory.CreateConnection();

        await using var command =
            connection.CreateCommand();

        command.CommandText = """
            SELECT RolId
            FROM dbo.Rol
            WHERE Nombre = @RoleName
            AND Estado = 1;
            """;

        AddParameter(
            command,
            "@RoleName",
            roleName.Trim(),
            DbType.String,
            50);

        var result = await command.ExecuteScalarAsync();

        return result is null || result == DBNull.Value
            ? null
            : Convert.ToInt32(result);
    }

    public async Task<IReadOnlyList<string>>
        GetActiveRoleNamesAsync()
    {
        await using var connection =
            _connectionFactory.CreateConnection();

        await using var command =
            connection.CreateCommand();

        command.CommandText = """
            SELECT Nombre
            FROM dbo.Rol
            WHERE Estado = 1
            ORDER BY Nombre ASC;
            """;

        var roles = new List<string>();

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            roles.Add(reader.GetString(
                reader.GetOrdinal("Nombre")));
        }

        return roles;
    }

    public async Task<Guid> CreatePendingAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        await using var connection =
            _connectionFactory.CreateConnection();

        await using var command =
            connection.CreateCommand();

        command.CommandText = """
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
            OUTPUT INSERTED.PublicId
            VALUES
            (
                @FirstName,
                @LastName,
                @Email,
                @Username,
                @FullName,
                NULL,
                @RoleId,
                @Status,
                @ActivationTokenHash,
                @ActivationTokenExpiresAt,
                NULL,
                @CreatedByUserId,
                NULL
            );
            """;

        AddParameter(
            command,
            "@FirstName",
            user.FirstName.Trim(),
            DbType.String,
            100);

        AddParameter(
            command,
            "@LastName",
            user.LastName.Trim(),
            DbType.String,
            100);

        AddParameter(
            command,
            "@Email",
            user.Email.Trim(),
            DbType.String,
            254);

        AddParameter(
            command,
            "@Username",
            user.Username.Trim(),
            DbType.String,
            50);

        AddParameter(
            command,
            "@FullName",
            user.FullName.Trim(),
            DbType.String,
            150);

        AddParameter(
            command,
            "@RoleId",
            user.RoleId,
            DbType.Int32);

        AddParameter(
            command,
            "@Status",
            UserStatuses.Pending,
            DbType.String,
            20);

        AddParameter(
            command,
            "@ActivationTokenHash",
            user.ActivationTokenHash
                ?? throw new InvalidOperationException(
                    "El token de activación es obligatorio."),
            DbType.String,
            200);

        AddParameter(
            command,
            "@ActivationTokenExpiresAt",
            user.ActivationTokenExpiresAt
                ?? throw new InvalidOperationException(
                    "La expiración del token es obligatoria."),
            DbType.DateTime2);

        AddParameter(
            command,
            "@CreatedByUserId",
            user.CreatedByUserId.HasValue
                ? user.CreatedByUserId.Value
                : DBNull.Value,
            DbType.Int32);

        var result = await command.ExecuteScalarAsync();

        return (Guid)(result
            ?? throw new InvalidOperationException(
                "No fue posible obtener el identificador del usuario."));
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

    public async Task<bool> UsernameExistsAsync(string username)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        await using var connection =
            _connectionFactory.CreateConnection();

        await using var command =
            connection.CreateCommand();

        command.CommandText = """
            SELECT COUNT(1)
            FROM dbo.Usuario
            WHERE NombreUsuario = @Username;
            """;

        AddParameter(
            command,
            "@Username",
            username.Trim(),
            DbType.String,
            50);

        return Convert.ToInt32(
            await command.ExecuteScalarAsync()) > 0;
    }

    public async Task<User?> GetByActivationTokenHashAsync(
    string tokenHash)
    {
        await using var connection =
            _connectionFactory.CreateConnection();

        await using var command =
            connection.CreateCommand();

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
            WHERE u.TokenActivacionHash = @TokenHash
            AND r.Estado = 1;
            """;

        AddParameter(
            command,
            "@TokenHash",
            tokenHash.Trim(),
            DbType.String,
            200);

        await using var reader =
            await command.ExecuteReaderAsync();

        return await reader.ReadAsync()
            ? MapUser(reader)
            : null;
    }

    public async Task<bool> ActivateAsync(
        int userId,
        string passwordHash,
        DateTime activatedAt)
    {
        await using var connection =
            _connectionFactory.CreateConnection();

        await using var command =
            connection.CreateCommand();

        command.CommandText = """
            UPDATE dbo.Usuario
            SET PasswordHash = @PasswordHash,
                Estado = @ActiveStatus,
                TokenActivacionFechaUso = @ActivatedAt,
                FechaModificacion = @ActivatedAt
            WHERE UsuarioId = @UserId
            AND Estado = @PendingStatus
            AND TokenActivacionFechaUso IS NULL
            AND TokenActivacionExpiracion > @ActivatedAt;
            """;

        AddParameter(
            command,
            "@PasswordHash",
            passwordHash,
            DbType.String,
            500);

        AddParameter(
            command,
            "@ActiveStatus",
            UserStatuses.Active,
            DbType.String,
            20);

        AddParameter(
            command,
            "@PendingStatus",
            UserStatuses.Pending,
            DbType.String,
            20);

        AddParameter(
            command,
            "@ActivatedAt",
            activatedAt,
            DbType.DateTime2);

        AddParameter(
            command,
            "@UserId",
            userId,
            DbType.Int32);

        return await command.ExecuteNonQueryAsync() == 1;
    }
}
using Libreria.Application.Ports.Secondary;
using Libreria.Domain;
using System.Data;
using System.Data.Common;

namespace Libreria.Infrastructure;

public class RolRepository : IRolRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public RolRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Rol>> ObtenerActivosAsync()
    {
        var roles = new List<Rol>();

        await using var connection = _connectionFactory.CreateConnection();

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync();

        await using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT RolId, Nombre, Descripcion, Estado
            FROM dbo.Rol
            WHERE Estado = 1
            ORDER BY Nombre;";

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            roles.Add(new Rol
            {
                RolId = reader.GetInt32(reader.GetOrdinal("RolId")),
                Nombre = reader.GetString(reader.GetOrdinal("Nombre")),

                Descripcion =
                    reader.IsDBNull(reader.GetOrdinal("Descripcion"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("Descripcion")),

                Estado = reader.GetBoolean(reader.GetOrdinal("Estado"))
            });
        }

        return roles;
    }
}
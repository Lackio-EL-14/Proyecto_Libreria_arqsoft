using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace Libreria.Servicio.Usuarios.Infrastructure.Connection;

public sealed class SqlConnectionFactory
{
    private readonly ConnectionStringSingleton _connectionStrings;

    public SqlConnectionFactory(
        ConnectionStringSingleton connectionStrings)
    {
        _connectionStrings = connectionStrings;
    }

    public DbConnection CreateConnection()
    {
        var connection =
            new SqlConnection(_connectionStrings.LibraryDb);

        connection.Open();

        return connection;
    }
}
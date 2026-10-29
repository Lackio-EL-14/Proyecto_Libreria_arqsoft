using System.Data.Common;
using Libreria.Application.Ports.Secondary;
using Microsoft.Data.SqlClient;

namespace Libreria.Infrastructure;

public sealed class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly ConnectionStringSingleton _connectionStrings;

    public SqlConnectionFactory(ConnectionStringSingleton connectionStrings)
    {
        _connectionStrings = connectionStrings;
    }

    public DbConnection CreateConnection()
    {
        var connection = new SqlConnection(_connectionStrings.LibreriaDb);
        connection.Open();
        return connection;
    }
}

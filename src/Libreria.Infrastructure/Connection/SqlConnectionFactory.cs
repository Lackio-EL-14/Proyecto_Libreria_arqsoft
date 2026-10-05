using Libreria.Application.Domain;
using Libreria.Application.Models;
using Libreria.Application.Ports.Secondary;
using Libreria.Application.Factories;
using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace Libreria.Infrastructure;

public class SqlConnectionFactory : IDbConnectionFactory
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

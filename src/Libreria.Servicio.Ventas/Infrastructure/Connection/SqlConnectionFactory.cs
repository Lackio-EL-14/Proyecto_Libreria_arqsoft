using Libreria.Servicio.Ventas.Application.Ports.Secondary;
using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace Libreria.Servicio.Ventas.Infrastructure;

public class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly ConnectionStringSingleton _connectionStrings;

    public SqlConnectionFactory(ConnectionStringSingleton connectionStrings)
    {
        _connectionStrings = connectionStrings;
    }

    public DbConnection CreateConnection()
    {
        var connection = new SqlConnection(_connectionStrings.VentasDb);
        connection.Open();
        return connection;
    }
}

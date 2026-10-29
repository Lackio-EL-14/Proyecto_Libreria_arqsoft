using Libreria.Domain;
using Libreria.Servicio.Ventas.Application.Models;
using Libreria.Servicio.Ventas.Application.Ports.Secondary;
using Libreria.Servicio.Ventas.Application.Factories;
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
        var connection = new SqlConnection(_connectionStrings.LibreriaDb);
        connection.Open();
        return connection;
    }
}

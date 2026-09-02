using System.Data;
using Microsoft.Data.SqlClient;

namespace Libreria.Web.Data;

/// <summary>
/// Implementación concreta de IDbConnectionFactory para SQL Server.
/// Si el día de mañana cambia el motor de base de datos, solo se
/// reemplaza esta clase; nada más en el proyecto debería cambiar.
/// </summary>
public class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public IDbConnection CreateConnection()
    {
        var connection = new SqlConnection(_connectionString);
        connection.Open();
        return connection;
    }
}

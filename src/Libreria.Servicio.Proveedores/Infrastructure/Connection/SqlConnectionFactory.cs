using System.Data.Common;
using Libreria.Servicio.Proveedores.Application.Ports.Secondary;
using Microsoft.Data.SqlClient;

namespace Libreria.Servicio.Proveedores.Infrastructure.Connection;

public sealed class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly ConnectionStringSingleton _cadenas;

    public SqlConnectionFactory(ConnectionStringSingleton cadenas)
    {
        _cadenas = cadenas;
    }

    public DbConnection CreateConnection()
    {
        var connection = new SqlConnection(_cadenas.ProveedoresDb);
        connection.Open();
        return connection;
    }
}

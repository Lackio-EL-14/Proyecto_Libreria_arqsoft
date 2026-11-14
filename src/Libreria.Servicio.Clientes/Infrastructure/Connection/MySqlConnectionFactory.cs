using System.Data.Common;
using Libreria.Servicio.Clientes.Application.Ports.Secondary;
using MySqlConnector;

namespace Libreria.Servicio.Clientes.Infrastructure.Connection;

/// <summary>Adapter del puerto <see cref="IDbConnectionFactory"/> para MySQL (MySqlConnector).</summary>
public class MySqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _cadenaConexion;

    public MySqlConnectionFactory(ConnectionStringSingleton connectionStrings)
    {
        var builder = new MySqlConnectionStringBuilder(connectionStrings.ClientesDb)
        {
            // Los UPDATE devuelven filas encontradas, no solo las cambiadas: así reactivar
            // un cliente ya activo no se confunde con «no existe».
            UseAffectedRows = false
        };

        _cadenaConexion = builder.ConnectionString;
    }

    public DbConnection CreateConnection()
    {
        var connection = new MySqlConnection(_cadenaConexion);
        connection.Open();
        return connection;
    }
}

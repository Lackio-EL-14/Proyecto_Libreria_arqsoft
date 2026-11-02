using System.Data.Common;
using Libreria.Servicio.Reportes.Application.Abstractions;
using Libreria.Servicio.Reportes.Application.Exceptions;
using Microsoft.Data.SqlClient;

namespace Libreria.Servicio.Reportes.Infrastructure.Connection;

public sealed class SqlConnectionFactory(ConnectionStringSingleton connectionStrings) : IDbConnectionFactory
{
    public async Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        string connectionString;
        try { connectionString = connectionStrings.LibreriaDb; }
        catch (InvalidOperationException exception)
        {
            throw new ReportDataUnavailableException("Falta configurar la conexión de Reportes.", exception);
        }

        var connection = new SqlConnection(connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}

using System.Data.Common;

namespace Libreria.Servicio.Reportes.Application.Abstractions;

public interface IDbConnectionFactory
{
    Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken);
}

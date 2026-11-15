using System.Data.Common;

namespace Libreria.Servicio.Ventas.Application.Ports.Secondary;

public interface IDbConnectionFactory
{
    DbConnection CreateConnection();
}

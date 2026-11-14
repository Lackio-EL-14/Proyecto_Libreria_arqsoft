using System.Data.Common;

namespace Libreria.Servicio.Clientes.Application.Ports.Secondary;

/// <summary>Puerto secundario: entrega una conexión abierta a la BD del servicio.</summary>
public interface IDbConnectionFactory
{
    DbConnection CreateConnection();
}

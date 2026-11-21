using System.Data.Common;

namespace Libreria.Servicio.Clientes.Infrastructure.Connection;

/// <summary>Detalle de infraestructura (ADO.NET): entrega una conexión abierta a la BD del servicio.</summary>
public interface IDbConnectionFactory
{
    DbConnection CreateConnection();
}

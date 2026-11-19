using System.Data.Common;
namespace Libreria.Servicio.Proveedores.Application.Ports.Secondary;

public interface IDbConnectionFactory { DbConnection CreateConnection(); }

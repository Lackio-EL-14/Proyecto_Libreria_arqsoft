using System.Data;

namespace Libreria.Web.Data;

/// <summary>
/// Abstracción sobre la creación de conexiones a la base de datos.
/// Permite que los PageModels dependan de una interfaz y no de una
/// implementación concreta (Principio de Inversión de Dependencias - SOLID).
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// Devuelve una conexión ya abierta, lista para usarse con SqlCommand.
    /// Quien la use es responsable de hacer Dispose() (usar "using").
    /// </summary>
    IDbConnection CreateConnection();
}

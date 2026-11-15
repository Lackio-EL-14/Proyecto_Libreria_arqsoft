using Libreria.Servicio.Clientes.Application.Ports.Secondary;

namespace Libreria.Servicio.Clientes.Application.Factories;

/// <summary>
/// Factory Method (copia de la de Ventas): cada fábrica concreta decide qué
/// repositorio crear; quien la usa solo conoce el puerto.
/// </summary>
public abstract class CrudRepositoryFactory<TEntity> where TEntity : class
{
    public abstract ICrudRepository<TEntity> CrearRepositorio();
}

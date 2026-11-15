using Libreria.Servicio.Productos.Application.Abstractions;

namespace Libreria.Servicio.Productos.Application.Factories;

public abstract class CrudRepositoryFactory<TEntity>
    where TEntity : class
{
    public abstract ICrudRepository<TEntity> CrearRepositorio();
}

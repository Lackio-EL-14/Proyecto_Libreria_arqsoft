using Libreria.Servicio.Proveedores.Application.Ports.Secondary;

namespace Libreria.Servicio.Proveedores.Application.Factories;

public abstract class CrudRepositoryFactory<TEntity> where TEntity : class
{
    public abstract ICrudRepository<TEntity> CrearRepositorio();
}

using Libreria.Web.Data.Repositories;

namespace Libreria.Web.Data.Factories
{
    public abstract class CrudRepositoryFactory<TEntity> where TEntity : class
    {
        public abstract ICrudRepository<TEntity> CrearRepositorio();
    }
}

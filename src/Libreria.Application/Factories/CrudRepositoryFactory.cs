using Libreria.Application.Domain;
using Libreria.Application.Models;
using Libreria.Application.Ports.Secondary;
using Libreria.Application.Factories;

namespace Libreria.Application.Factories
{
    public abstract class CrudRepositoryFactory<TEntity> where TEntity : class
    {
        public abstract ICrudRepository<TEntity> CrearRepositorio();
    }
}

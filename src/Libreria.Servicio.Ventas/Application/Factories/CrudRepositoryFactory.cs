using Libreria.Domain;
using Libreria.Servicio.Ventas.Application.Models;
using Libreria.Servicio.Ventas.Application.Ports.Secondary;
using Libreria.Servicio.Ventas.Application.Factories;

namespace Libreria.Servicio.Ventas.Application.Factories
{
    public abstract class CrudRepositoryFactory<TEntity> where TEntity : class
    {
        public abstract ICrudRepository<TEntity> CrearRepositorio();
    }
}

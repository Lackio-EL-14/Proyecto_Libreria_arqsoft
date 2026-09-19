using Libreria.Web.Data;
using Libreria.Web.Data.Repositories;
using Libreria.Web.Domain.Entities;

namespace Libreria.Web.Data.Factories
{
    public class CategoriaRepositoryFactory : CrudRepositoryFactory<Categoria>
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public CategoriaRepositoryFactory(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public override ICrudRepository<Categoria> CrearRepositorio()
        {
            // Retorna la instancia concreta
            return new CategoriaRepository(_connectionFactory);
        }
    }
}

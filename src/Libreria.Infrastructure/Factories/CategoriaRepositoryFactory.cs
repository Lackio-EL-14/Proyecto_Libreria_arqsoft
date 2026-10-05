using Libreria.Application.Domain;
using Libreria.Application.Models;
using Libreria.Application.Ports.Secondary;
using Libreria.Application.Factories;


namespace Libreria.Infrastructure
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

using Libreria.Application.Domain;
using Libreria.Application.Models;
using Libreria.Application.Ports.Secondary;
using Libreria.Application.Factories;

namespace Libreria.Infrastructure
{
    public class ProductoRepositoryFactory : CrudRepositoryFactory<Producto>
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public ProductoRepositoryFactory(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public override ICrudRepository<Producto> CrearRepositorio()
        {
            return new ProductoRepository(_connectionFactory);
        }
    }
}
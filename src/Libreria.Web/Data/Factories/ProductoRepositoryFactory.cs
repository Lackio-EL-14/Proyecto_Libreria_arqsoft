using Libreria.Web.Data.Repositories;
using Libreria.Web.Domain.Entities;

namespace Libreria.Web.Data.Factories
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
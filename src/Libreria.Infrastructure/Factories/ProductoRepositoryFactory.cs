using Libreria.Application.Domain;
using Libreria.Application.Models;
using Libreria.Application.Ports.Secondary;
using Libreria.Application.Factories;

namespace Libreria.Infrastructure
{
    public class ProductoRepositoryFactory : CrudRepositoryFactory<Producto>
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IUsuarioActual _usuarioActual;

        public ProductoRepositoryFactory(IDbConnectionFactory connectionFactory, IUsuarioActual usuarioActual)
        {
            _connectionFactory = connectionFactory;
            _usuarioActual = usuarioActual;
        }

        public override ICrudRepository<Producto> CrearRepositorio()
        {
            return new ProductoRepository(_connectionFactory, _usuarioActual);
        }
    }
}
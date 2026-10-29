using Libreria.Domain;
using Libreria.Servicio.Ventas.Application.Models;
using Libreria.Servicio.Ventas.Application.Ports.Secondary;
using Libreria.Servicio.Ventas.Application.Factories;

namespace Libreria.Servicio.Ventas.Infrastructure
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
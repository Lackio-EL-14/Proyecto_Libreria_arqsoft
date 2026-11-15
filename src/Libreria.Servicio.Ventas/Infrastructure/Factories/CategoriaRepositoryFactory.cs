using Libreria.Servicio.Ventas.Domain;
using Libreria.Servicio.Ventas.Application.Models;
using Libreria.Servicio.Ventas.Application.Ports.Secondary;
using Libreria.Servicio.Ventas.Application.Factories;


namespace Libreria.Servicio.Ventas.Infrastructure
{
    public class CategoriaRepositoryFactory : CrudRepositoryFactory<Categoria>
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IUsuarioActual _usuarioActual;

        public CategoriaRepositoryFactory(IDbConnectionFactory connectionFactory, IUsuarioActual usuarioActual)
        {
            _connectionFactory = connectionFactory;
            _usuarioActual = usuarioActual;
        }

        public override ICrudRepository<Categoria> CrearRepositorio()
        {
            // Retorna la instancia concreta
            return new CategoriaRepository(_connectionFactory, _usuarioActual);
        }
    }
}

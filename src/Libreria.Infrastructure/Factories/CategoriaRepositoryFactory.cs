using Libreria.Application.Domain;
using Libreria.Application.Models;
using Libreria.Application.Ports.Secondary;
using Libreria.Application.Factories;


namespace Libreria.Infrastructure
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

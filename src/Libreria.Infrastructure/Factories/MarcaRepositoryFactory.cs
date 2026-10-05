using Libreria.Application.Domain;
using Libreria.Application.Models;
using Libreria.Application.Ports.Secondary;
using Libreria.Application.Factories;

namespace Libreria.Infrastructure;

public class MarcaRepositoryFactory : CrudRepositoryFactory<Marca>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MarcaRepositoryFactory(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public override ICrudRepository<Marca> CrearRepositorio()
    {
        return new MarcaRepository(_connectionFactory);
    }
}
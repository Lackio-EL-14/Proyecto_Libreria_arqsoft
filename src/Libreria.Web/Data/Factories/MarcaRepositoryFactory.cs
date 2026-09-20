using Libreria.Web.Data.Repositories;
using Libreria.Web.Domain.Entities;

namespace Libreria.Web.Data.Factories;

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
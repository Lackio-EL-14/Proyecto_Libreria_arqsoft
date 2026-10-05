using Libreria.Application.Factories;
using Libreria.Application.Ports.Secondary;

namespace Libreria.Infrastructure;

public class SqlUsuarioRepositoryFactory : UsuarioRepositoryFactory
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SqlUsuarioRepositoryFactory(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public override IUsuarioRepository CrearRepositorio()
    {
        return new UsuarioRepository(_connectionFactory);
    }
}

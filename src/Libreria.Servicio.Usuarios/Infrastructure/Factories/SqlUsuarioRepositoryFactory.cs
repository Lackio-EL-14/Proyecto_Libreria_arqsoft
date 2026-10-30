using Libreria.Servicio.Usuarios.Application.Abstractions;
using Libreria.Servicio.Usuarios.Application.Factories;
using Libreria.Servicio.Usuarios.Infrastructure.Connection;
using Libreria.Servicio.Usuarios.Infrastructure.Repositories;

namespace Libreria.Servicio.Usuarios.Infrastructure.Factories;

public sealed class SqlUsuarioRepositoryFactory : UsuarioRepositoryFactory
{
    private readonly SqlConnectionFactory _connectionFactory;

    public SqlUsuarioRepositoryFactory(
        SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public override IUserRepository CreateRepository()
    {
        return new UserRepository(_connectionFactory);
    }
}
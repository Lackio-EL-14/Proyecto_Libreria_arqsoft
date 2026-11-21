using Libreria.Servicio.Clientes.Application.Factories;
using Libreria.Servicio.Clientes.Application.Ports.Secondary;
using Libreria.Servicio.Clientes.Infrastructure.Repositories;
using Libreria.Servicio.Clientes.Infrastructure.Connection;

namespace Libreria.Servicio.Clientes.Infrastructure.Factories;

/// <summary>Creador concreto del Factory Method: entrega el repositorio de clientes sobre MySQL.</summary>
public class MySqlClienteRepositoryFactory : ClienteRepositoryFactory
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IUsuarioActual _usuarioActual;

    public MySqlClienteRepositoryFactory(IDbConnectionFactory connectionFactory, IUsuarioActual usuarioActual)
    {
        _connectionFactory = connectionFactory;
        _usuarioActual = usuarioActual;
    }

    public override IClienteRepository CrearRepositorio() =>
        new ClienteRepository(_connectionFactory, _usuarioActual);
}

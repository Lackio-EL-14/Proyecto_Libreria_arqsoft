using Libreria.Application.Domain;
using Libreria.Application.Models;
using Libreria.Application.Ports.Secondary;
using Libreria.Application.Factories;

namespace Libreria.Infrastructure;

public class MarcaRepositoryFactory : CrudRepositoryFactory<Marca>
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IUsuarioActual _usuarioActual;

    public MarcaRepositoryFactory(IDbConnectionFactory connectionFactory, IUsuarioActual usuarioActual)
    {
        _connectionFactory = connectionFactory;
        _usuarioActual = usuarioActual;
    }

    public override ICrudRepository<Marca> CrearRepositorio()
    {
        return new MarcaRepository(_connectionFactory, _usuarioActual);
    }
}
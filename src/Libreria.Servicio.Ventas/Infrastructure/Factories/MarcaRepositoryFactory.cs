using Libreria.Servicio.Ventas.Domain;
using Libreria.Servicio.Ventas.Application.Models;
using Libreria.Servicio.Ventas.Application.Ports.Secondary;
using Libreria.Servicio.Ventas.Application.Factories;

namespace Libreria.Servicio.Ventas.Infrastructure;

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
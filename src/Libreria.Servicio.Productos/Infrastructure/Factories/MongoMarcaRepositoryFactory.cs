using Libreria.Servicio.Productos.Application.Abstractions;
using Libreria.Servicio.Productos.Application.Factories;
using Libreria.Servicio.Productos.Domain.Entities;
using Libreria.Servicio.Productos.Infrastructure.Mongo;
using Libreria.Servicio.Productos.Infrastructure.Repositories;

namespace Libreria.Servicio.Productos.Infrastructure.Factories;

public sealed class MongoMarcaRepositoryFactory
    : CrudRepositoryFactory<Marca>
{
    private readonly MongoContext _context;
    private readonly IUsuarioActual _usuarioActual;

    public MongoMarcaRepositoryFactory(
        MongoContext context,
        IUsuarioActual usuarioActual)
    {
        _context = context;
        _usuarioActual = usuarioActual;
    }

    public override ICrudRepository<Marca> CrearRepositorio()
    {
        return new MongoMarcaRepository(
            _context,
            _usuarioActual);
    }
}
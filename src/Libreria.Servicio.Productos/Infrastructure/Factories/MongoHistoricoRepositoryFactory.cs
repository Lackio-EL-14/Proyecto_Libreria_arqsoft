using Libreria.Servicio.Productos.Application.Abstractions;
using Libreria.Servicio.Productos.Application.Factories;
using Libreria.Servicio.Productos.Infrastructure.Mongo;
using Libreria.Servicio.Productos.Infrastructure.Repositories;

namespace Libreria.Servicio.Productos.Infrastructure.Factories;

public sealed class MongoHistoricoRepositoryFactory
    : HistoricoRepositoryFactory
{
    private readonly MongoContext _context;

    public MongoHistoricoRepositoryFactory(MongoContext context)
    {
        _context = context;
    }

    public override IHistoricoCostoRepository CrearRepositorio()
    {
        return new MongoHistoricoCostoRepository(_context);
    }
}
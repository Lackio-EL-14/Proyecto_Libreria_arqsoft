using Libreria.Servicio.Productos.Application.Abstractions;
using Libreria.Servicio.Productos.Domain.Entities;
using Libreria.Servicio.Productos.Infrastructure.Mongo;
using MongoDB.Driver;

namespace Libreria.Servicio.Productos.Infrastructure.Repositories;

public sealed class MongoHistoricoCostoRepository
    : IHistoricoCostoRepository
{
    private readonly MongoContext _context;

    public MongoHistoricoCostoRepository(MongoContext context)
    {
        _context = context;
    }

    public async Task<Producto?> ObtenerProductoAsync(Guid publicId)
    {
        return await _context.Productos
            .Find(producto => producto.PublicId == publicId)
            .FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyList<HistoricoCosto>>
        ObtenerHistoricoAsync(Guid productoPublicId)
    {
        var producto = await _context.Productos
            .Find(producto =>
                producto.PublicId == productoPublicId)
            .FirstOrDefaultAsync();

        if (producto is null)
            return [];

        return producto.HistoricoCostos
            .OrderByDescending(item => item.FechaVigencia)
            .ToList();
    }
}
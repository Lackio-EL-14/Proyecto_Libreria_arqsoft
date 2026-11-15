using Libreria.Servicio.Productos.Application.Abstractions;
using Libreria.Servicio.Productos.Domain.Entities;
using Libreria.Servicio.Productos.Infrastructure.Mongo;
using MongoDB.Driver;

namespace Libreria.Servicio.Productos.Infrastructure.Repositories;

public sealed class MongoCatalogoProductoRepository
    : ICatalogoProductoRepository
{
    private readonly MongoContext _context;

    public MongoCatalogoProductoRepository(MongoContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Categoria>>
        ObtenerCategoriasActivasAsync()
    {
        return await _context.Categorias
            .Find(categoria => categoria.Estado)
            .SortBy(categoria => categoria.Nombre)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Marca>>
        ObtenerMarcasActivasAsync()
    {
        return await _context.Marcas
            .Find(marca => marca.Estado)
            .SortBy(marca => marca.Nombre)
            .ToListAsync();
    }

    public async Task<bool> ExisteCategoriaActivaAsync(Guid publicId)
    {
        var filtro =
            Builders<Categoria>.Filter.Eq(
                categoria => categoria.PublicId,
                publicId)
            & Builders<Categoria>.Filter.Eq(
                categoria => categoria.Estado,
                true);

        return await _context.Categorias
            .CountDocumentsAsync(filtro) > 0;
    }

    public async Task<bool> ExisteMarcaActivaAsync(Guid publicId)
    {
        var filtro =
            Builders<Marca>.Filter.Eq(
                marca => marca.PublicId,
                publicId)
            & Builders<Marca>.Filter.Eq(
                marca => marca.Estado,
                true);

        return await _context.Marcas
            .CountDocumentsAsync(filtro) > 0;
    }
}
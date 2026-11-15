using System.Text.RegularExpressions;
using Libreria.Servicio.Productos.Application.Abstractions;
using Libreria.Servicio.Productos.Domain.Entities;
using Libreria.Servicio.Productos.Infrastructure.Mongo;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Libreria.Servicio.Productos.Infrastructure.Repositories;

public sealed class MongoMarcaRepository : ICrudRepository<Marca>
{
    private readonly MongoContext _context;
    private readonly IUsuarioActual _usuarioActual;

    public MongoMarcaRepository(
        MongoContext context,
        IUsuarioActual usuarioActual)
    {
        _context = context;
        _usuarioActual = usuarioActual;
    }

    public async Task<IReadOnlyList<Marca>> ObtenerActivasAsync(
        string? busqueda = null)
    {
        var filtro = Builders<Marca>.Filter.Eq(
            marca => marca.Estado,
            true);

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var regex = new BsonRegularExpression(
                Regex.Escape(busqueda.Trim()),
                "i");

            var filtroBusqueda =
                Builders<Marca>.Filter.Regex(
                    marca => marca.Nombre,
                    regex)
                | Builders<Marca>.Filter.Regex(
                    marca => marca.PaisOrigen,
                    regex);

            filtro &= filtroBusqueda;
        }

        return await _context.Marcas
            .Find(filtro)
            .SortBy(marca => marca.Nombre)
            .ToListAsync();
    }

    public async Task<Marca?> ObtenerPorPublicIdAsync(
        Guid publicId,
        bool estadoEsperado)
    {
        var filtro =
            Builders<Marca>.Filter.Eq(
                marca => marca.PublicId,
                publicId)
            & Builders<Marca>.Filter.Eq(
                marca => marca.Estado,
                estadoEsperado);

        return await _context.Marcas
            .Find(filtro)
            .FirstOrDefaultAsync();
    }

    public async Task CrearAsync(Marca entidad)
    {
        entidad.PublicId =
            entidad.PublicId == Guid.Empty
                ? Guid.NewGuid()
                : entidad.PublicId;

        entidad.Estado = true;
        entidad.FechaCreacion = DateTime.UtcNow;
        entidad.FechaModificacion = null;
        entidad.UsuarioCreacionId = _usuarioActual.UsuarioId;
        entidad.UsuarioModificacionId = null;

        await _context.Marcas.InsertOneAsync(entidad);
    }

    public async Task<bool> ActualizarAsync(Marca entidad)
    {
        var filtro =
            Builders<Marca>.Filter.Eq(
                marca => marca.PublicId,
                entidad.PublicId)
            & Builders<Marca>.Filter.Eq(
                marca => marca.Estado,
                true);

        var actualizacion = Builders<Marca>.Update
            .Set(marca => marca.Nombre, entidad.Nombre)
            .Set(marca => marca.Descripcion, entidad.Descripcion)
            .Set(marca => marca.PaisOrigen, entidad.PaisOrigen)
            .Set(marca => marca.SitioWeb, entidad.SitioWeb)
            .Set(marca => marca.FechaModificacion, DateTime.UtcNow)
            .Set(
                marca => marca.UsuarioModificacionId,
                _usuarioActual.UsuarioId);

        var resultado = await _context.Marcas.UpdateOneAsync(
            filtro,
            actualizacion);

        return resultado.ModifiedCount == 1;
    }

    public async Task<bool> CambiarEstadoAsync(
        Guid publicId,
        bool nuevoEstado)
    {
        var filtro =
            Builders<Marca>.Filter.Eq(
                marca => marca.PublicId,
                publicId)
            & Builders<Marca>.Filter.Eq(
                marca => marca.Estado,
                !nuevoEstado);

        var actualizacion = Builders<Marca>.Update
            .Set(marca => marca.Estado, nuevoEstado)
            .Set(marca => marca.FechaModificacion, DateTime.UtcNow)
            .Set(
                marca => marca.UsuarioModificacionId,
                _usuarioActual.UsuarioId);

        var resultado = await _context.Marcas.UpdateOneAsync(
            filtro,
            actualizacion);

        return resultado.ModifiedCount == 1;
    }

    public async Task<bool> ExisteValorAsync(
        string campo,
        string valor,
        Guid? excluirPublicId = null)
    {
        if (campo != "Nombre")
        {
            throw new ArgumentException(
                $"El campo '{campo}' no puede utilizarse para comprobar duplicados.",
                nameof(campo));
        }

        var filtro = Builders<Marca>.Filter.Eq(
            marca => marca.Nombre,
            valor);

        if (excluirPublicId.HasValue)
        {
            filtro &= Builders<Marca>.Filter.Ne(
                marca => marca.PublicId,
                excluirPublicId.Value);
        }

        return await _context.Marcas.CountDocumentsAsync(filtro) > 0;
    }

    public async Task<bool> TieneRelacionesAsync(Guid publicId)
    {
        var filtro =
            Builders<Producto>.Filter.Eq(
                producto => producto.Marca.PublicId,
                publicId)
            & Builders<Producto>.Filter.Eq(
                producto => producto.Estado,
                true);

        return await _context.Productos.CountDocumentsAsync(filtro) > 0;
    }
}
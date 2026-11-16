using System.Text.RegularExpressions;
using Libreria.Servicio.Productos.Application.Abstractions;
using Libreria.Servicio.Productos.Domain.Entities;
using Libreria.Servicio.Productos.Infrastructure.Mongo;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Libreria.Servicio.Productos.Infrastructure.Repositories;

public sealed class MongoCategoriaRepository : ICrudRepository<Categoria>
{
    private readonly MongoContext _context;
    private readonly IUsuarioActual _usuarioActual;

    public MongoCategoriaRepository(
        MongoContext context,
        IUsuarioActual usuarioActual)
    {
        _context = context;
        _usuarioActual = usuarioActual;
    }

    public async Task<IReadOnlyList<Categoria>> ObtenerActivasAsync(
        string? busqueda = null)
    {
        var filtro = Builders<Categoria>.Filter.Eq(
            categoria => categoria.Estado,
            true);

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var texto = Regex.Escape(busqueda.Trim());
            var regex = new BsonRegularExpression(texto, "i");

            var filtroBusqueda =
                Builders<Categoria>.Filter.Regex(
                    categoria => categoria.Codigo,
                    regex)
                | Builders<Categoria>.Filter.Regex(
                    categoria => categoria.Nombre,
                    regex)
                | Builders<Categoria>.Filter.Regex(
                    categoria => categoria.Ubicacion,
                    regex);

            filtro &= filtroBusqueda;
        }

        return await _context.Categorias
            .Find(filtro)
            .SortBy(categoria => categoria.Nombre)
            .ToListAsync();
    }

    public async Task<Categoria?> ObtenerPorPublicIdAsync(
        Guid publicId,
        bool estadoEsperado)
    {
        var filtro =
            Builders<Categoria>.Filter.Eq(
                categoria => categoria.PublicId,
                publicId)
            & Builders<Categoria>.Filter.Eq(
                categoria => categoria.Estado,
                estadoEsperado);

        return await _context.Categorias
            .Find(filtro)
            .FirstOrDefaultAsync();
    }

    public async Task CrearAsync(Categoria entidad)
    {
        entidad.PublicId =
            entidad.PublicId == Guid.Empty
                ? Guid.NewGuid()
                : entidad.PublicId;

        entidad.Codigo = await GenerarCodigoAsync();
        entidad.Estado = true;
        entidad.FechaCreacion = DateTime.UtcNow;
        entidad.FechaModificacion = null;
        entidad.UsuarioCreacionId = _usuarioActual.UsuarioId;
        entidad.UsuarioModificacionId = null;

        await _context.Categorias.InsertOneAsync(entidad);
    }

    public async Task<bool> ActualizarAsync(Categoria entidad)
    {
        var filtro =
            Builders<Categoria>.Filter.Eq(
                categoria => categoria.PublicId,
                entidad.PublicId)
            & Builders<Categoria>.Filter.Eq(
                categoria => categoria.Estado,
                true);

        var actualizacion = Builders<Categoria>.Update
            .Set(categoria => categoria.Nombre, entidad.Nombre)
            .Set(categoria => categoria.Descripcion, entidad.Descripcion)
            .Set(categoria => categoria.Ubicacion, entidad.Ubicacion)
            .Set(categoria => categoria.FechaModificacion, DateTime.UtcNow)
            .Set(
                categoria => categoria.UsuarioModificacionId,
                _usuarioActual.UsuarioId);

        var resultado = await _context.Categorias.UpdateOneAsync(
            filtro,
            actualizacion);

        return resultado.ModifiedCount == 1;
    }

    public async Task<bool> CambiarEstadoAsync(
        Guid publicId,
        bool nuevoEstado)
    {
        var filtro =
            Builders<Categoria>.Filter.Eq(
                categoria => categoria.PublicId,
                publicId)
            & Builders<Categoria>.Filter.Eq(
                categoria => categoria.Estado,
                !nuevoEstado);

        var actualizacion = Builders<Categoria>.Update
            .Set(categoria => categoria.Estado, nuevoEstado)
            .Set(categoria => categoria.FechaModificacion, DateTime.UtcNow)
            .Set(
                categoria => categoria.UsuarioModificacionId,
                _usuarioActual.UsuarioId);

        var resultado = await _context.Categorias.UpdateOneAsync(
            filtro,
            actualizacion);

        return resultado.ModifiedCount == 1;
    }

    public async Task<bool> ExisteValorAsync(
        string campo,
        string valor,
        Guid? excluirPublicId = null)
    {
        FilterDefinition<Categoria> filtro = campo switch
        {
            "Nombre" => Builders<Categoria>.Filter.Eq(
                categoria => categoria.Nombre,
                valor),

            "Codigo" => Builders<Categoria>.Filter.Eq(
                categoria => categoria.Codigo,
                valor),

            _ => throw new ArgumentException(
                $"El campo '{campo}' no puede utilizarse para comprobar duplicados.",
                nameof(campo))
        };

        if (excluirPublicId.HasValue)
        {
            filtro &= Builders<Categoria>.Filter.Ne(
                categoria => categoria.PublicId,
                excluirPublicId.Value);
        }

        return await _context.Categorias.CountDocumentsAsync(filtro) > 0;
    }

    public async Task<bool> TieneRelacionesAsync(Guid publicId)
    {
        var filtro =
            Builders<Producto>.Filter.Eq(
                producto => producto.Categoria.PublicId,
                publicId)
            & Builders<Producto>.Filter.Eq(
                producto => producto.Estado,
                true);

        return await _context.Productos.CountDocumentsAsync(filtro) > 0;
    }

    private async Task<string> GenerarCodigoAsync()
    {
        var filtro = Builders<BsonDocument>.Filter.Eq(
            "_id",
            "categoria");

        var actualizacion =
            Builders<BsonDocument>.Update.Inc("valor", 1);

        var opciones = new FindOneAndUpdateOptions<BsonDocument>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After
        };

        var contador = await _context.Contadores.FindOneAndUpdateAsync(
            filtro,
            actualizacion,
            opciones);

        var numero = contador["valor"].ToInt32();

        return $"CAT-{numero:D3}";
    }
}
using System.Text.RegularExpressions;
using Libreria.Servicio.Productos.Application.Abstractions;
using Libreria.Servicio.Productos.Domain.Entities;
using Libreria.Servicio.Productos.Infrastructure.Mongo;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Libreria.Servicio.Productos.Infrastructure.Repositories;

public sealed class MongoProductoRepository : IProductoRepository
{
    private readonly MongoContext _context;
    private readonly IUsuarioActual _usuarioActual;

    public MongoProductoRepository(
        MongoContext context,
        IUsuarioActual usuarioActual)
    {
        _context = context;
        _usuarioActual = usuarioActual;
    }

    public async Task<IReadOnlyList<Producto>> ObtenerActivasAsync(
        string? busqueda = null)
    {
        var filtro = Builders<Producto>.Filter.Eq(
            producto => producto.Estado,
            true);

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var regex = new BsonRegularExpression(
                Regex.Escape(busqueda.Trim()),
                "i");

            filtro &= Builders<Producto>.Filter.Regex(
                    producto => producto.Nombre,
                    regex)
                | Builders<Producto>.Filter.Regex(
                    producto => producto.DescripcionEspecifica,
                    regex);
        }

        return await _context.Productos
            .Find(filtro)
            .SortBy(producto => producto.Nombre)
            .ToListAsync();
    }

    public async Task<Producto?> ObtenerPorPublicIdAsync(
        Guid publicId,
        bool estadoEsperado)
    {
        var filtro =
            Builders<Producto>.Filter.Eq(
                producto => producto.PublicId,
                publicId)
            & Builders<Producto>.Filter.Eq(
                producto => producto.Estado,
                estadoEsperado);

        return await _context.Productos
            .Find(filtro)
            .FirstOrDefaultAsync();
    }

    public async Task CrearAsync(Producto entidad)
    {
        var usuarioId = ObtenerUsuarioId();

        entidad.PublicId =
            entidad.PublicId == Guid.Empty
                ? Guid.NewGuid()
                : entidad.PublicId;

        entidad.Estado = true;
        entidad.FechaCreacion = DateTime.UtcNow;
        entidad.FechaModificacion = null;
        entidad.UsuarioCreacionId = usuarioId;
        entidad.UsuarioModificacionId = null;

        entidad.HistoricoCostos =
        [
            new HistoricoCosto
            {
                CostoAdquisicion = entidad.CostoAdquisicionActual,
                FechaVencimiento = entidad.FechaVencimiento,
                TipoCambioUsd = null,
                Motivo = "Registro inicial",
                FechaVigencia = entidad.FechaCreacion,
                UsuarioCreacionId = usuarioId
            }
        ];

        await _context.Productos.InsertOneAsync(entidad);
    }

    public async Task<bool> ActualizarAsync(Producto entidad)
    {
        var actual = await ObtenerPorPublicIdAsync(
            entidad.PublicId,
            true);

        if (actual is null)
            return false;

        var usuarioId = ObtenerUsuarioId();
        var fechaModificacion = DateTime.UtcNow;

        var filtro =
            Builders<Producto>.Filter.Eq(
                producto => producto.PublicId,
                entidad.PublicId)
            & Builders<Producto>.Filter.Eq(
                producto => producto.Estado,
                true);

        var actualizaciones = new List<UpdateDefinition<Producto>>
        {
            Builders<Producto>.Update.Set(
                producto => producto.Nombre,
                entidad.Nombre),

            Builders<Producto>.Update.Set(
                producto => producto.DescripcionEspecifica,
                entidad.DescripcionEspecifica),

            Builders<Producto>.Update.Set(
                producto => producto.EsPerecedero,
                entidad.EsPerecedero),

            Builders<Producto>.Update.Set(
                producto => producto.FechaVencimiento,
                entidad.FechaVencimiento),

            Builders<Producto>.Update.Set(
                producto => producto.Stock,
                entidad.Stock),

            Builders<Producto>.Update.Set(
                producto => producto.PrecioVenta,
                entidad.PrecioVenta),

            Builders<Producto>.Update.Set(
                producto => producto.CostoAdquisicionActual,
                entidad.CostoAdquisicionActual),

            Builders<Producto>.Update.Set(
                producto => producto.Categoria,
                entidad.Categoria),

            Builders<Producto>.Update.Set(
                producto => producto.Marca,
                entidad.Marca),

            Builders<Producto>.Update.Set(
                producto => producto.FechaModificacion,
                fechaModificacion),

            Builders<Producto>.Update.Set(
                producto => producto.UsuarioModificacionId,
                usuarioId)
        };

        if (actual.CostoAdquisicionActual != entidad.CostoAdquisicionActual)
        {
            actualizaciones.Add(
                Builders<Producto>.Update.Push(
                    producto => producto.HistoricoCostos,
                    new HistoricoCosto
                    {
                        CostoAdquisicion = entidad.CostoAdquisicionActual,
                        FechaVencimiento = entidad.FechaVencimiento,
                        TipoCambioUsd = null,
                        Motivo = "Edición manual",
                        FechaVigencia = fechaModificacion,
                        UsuarioCreacionId = usuarioId
                    }));
        }

        var actualizacion =
            Builders<Producto>.Update.Combine(actualizaciones);

        var resultado = await _context.Productos.UpdateOneAsync(
            filtro,
            actualizacion);

        return resultado.ModifiedCount == 1;
    }

    public async Task<bool> CambiarEstadoAsync(
        Guid publicId,
        bool nuevoEstado)
    {
        var usuarioId = ObtenerUsuarioId();

        var filtro =
            Builders<Producto>.Filter.Eq(
                producto => producto.PublicId,
                publicId)
            & Builders<Producto>.Filter.Eq(
                producto => producto.Estado,
                !nuevoEstado);

        var actualizacion = Builders<Producto>.Update
            .Set(producto => producto.Estado, nuevoEstado)
            .Set(producto => producto.FechaModificacion, DateTime.UtcNow)
            .Set(
                producto => producto.UsuarioModificacionId,
                usuarioId);

        var resultado = await _context.Productos.UpdateOneAsync(
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

        var filtro = Builders<Producto>.Filter.Eq(
            producto => producto.Nombre,
            valor);

        if (excluirPublicId.HasValue)
        {
            filtro &= Builders<Producto>.Filter.Ne(
                producto => producto.PublicId,
                excluirPublicId.Value);
        }

        return await _context.Productos.CountDocumentsAsync(filtro) > 0;
    }

    public Task<bool> TieneRelacionesAsync(Guid publicId)
    {
        // El histórico está embebido dentro del mismo producto,
        // por lo que no constituye una relación externa que impida
        // realizar la baja lógica.
        return Task.FromResult(false);
    }

    public async Task<Producto?> ObtenerParaVentaAsync(Guid publicId)
    {
        return await ObtenerPorPublicIdAsync(publicId, true);
    }

    public async Task<bool> DescontarStockAsync(
        Guid publicId,
        int cantidad)
    {
        if (cantidad <= 0)
            return false;

        var usuarioId = ObtenerUsuarioId();

        var filtro =
            Builders<Producto>.Filter.Eq(
                producto => producto.PublicId,
                publicId)
            & Builders<Producto>.Filter.Eq(
                producto => producto.Estado,
                true)
            & Builders<Producto>.Filter.Gte(
                producto => producto.Stock,
                cantidad);

        var actualizacion = Builders<Producto>.Update
            .Inc(producto => producto.Stock, -cantidad)
            .Set(producto => producto.FechaModificacion, DateTime.UtcNow)
            .Set(
                producto => producto.UsuarioModificacionId,
                usuarioId);

        var resultado = await _context.Productos.UpdateOneAsync(
            filtro,
            actualizacion);

        return resultado.ModifiedCount == 1;
    }

    public async Task<bool> RestaurarStockAsync(
        Guid publicId,
        int cantidad)
    {
        if (cantidad <= 0)
            return false;

        var usuarioId = ObtenerUsuarioId();

        var filtro =
            Builders<Producto>.Filter.Eq(
                producto => producto.PublicId,
                publicId)
            & Builders<Producto>.Filter.Eq(
                producto => producto.Estado,
                true);

        var actualizacion = Builders<Producto>.Update
            .Inc(producto => producto.Stock, cantidad)
            .Set(producto => producto.FechaModificacion, DateTime.UtcNow)
            .Set(
                producto => producto.UsuarioModificacionId,
                usuarioId);

        var resultado = await _context.Productos.UpdateOneAsync(
            filtro,
            actualizacion);

        return resultado.ModifiedCount == 1;
    }

    public async Task ActualizarCategoriaEmbebidaAsync(
        Guid categoriaPublicId,
        string nuevoNombre)
    {
        var filtro = Builders<Producto>.Filter.Eq(
            producto => producto.Categoria.PublicId,
            categoriaPublicId);

        var actualizacion = Builders<Producto>.Update.Set(
            producto => producto.Categoria.Nombre,
            nuevoNombre);

        await _context.Productos.UpdateManyAsync(
            filtro,
            actualizacion);
    }

    public async Task ActualizarMarcaEmbebidaAsync(
        Guid marcaPublicId,
        string nuevoNombre)
    {
        var filtro = Builders<Producto>.Filter.Eq(
            producto => producto.Marca.PublicId,
            marcaPublicId);

        var actualizacion = Builders<Producto>.Update.Set(
            producto => producto.Marca.Nombre,
            nuevoNombre);

        await _context.Productos.UpdateManyAsync(
            filtro,
            actualizacion);
    }

    private Guid ObtenerUsuarioId()
    {
        return _usuarioActual.UsuarioId
            ?? throw new InvalidOperationException(
                "No existe un usuario actual para registrar la auditoría.");
    }
}
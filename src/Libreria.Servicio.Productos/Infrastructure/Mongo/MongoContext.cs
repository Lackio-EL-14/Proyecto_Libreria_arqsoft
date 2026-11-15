using Libreria.Servicio.Productos.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Libreria.Servicio.Productos.Infrastructure.Mongo;

public sealed class MongoContext
{
    public IMongoCollection<Producto> Productos { get; }
    public IMongoCollection<Categoria> Categorias { get; }
    public IMongoCollection<Marca> Marcas { get; }
    public IMongoCollection<BsonDocument> Contadores { get; }

    public MongoContext(IMongoDatabase database)
    {
        Productos = database.GetCollection<Producto>("productos");
        Categorias = database.GetCollection<Categoria>("categorias");
        Marcas = database.GetCollection<Marca>("marcas");
        Contadores = database.GetCollection<BsonDocument>("contadores");
    }
}
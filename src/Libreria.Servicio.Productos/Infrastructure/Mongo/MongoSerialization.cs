using System.Threading;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace Libreria.Servicio.Productos.Infrastructure.Mongo;

public static class MongoSerialization
{
    private static int _configurada;

    public static void Configurar()
    {
        if (Interlocked.Exchange(ref _configurada, 1) == 1)
            return;

        // Los scripts semilla guardan los GUID como string.
        BsonSerializer.RegisterSerializer(
            new GuidSerializer(BsonType.String));

        var convenciones = new ConventionPack
        {
            new CamelCaseElementNameConvention(),
            new IgnoreExtraElementsConvention(true)
        };

        ConventionRegistry.Register(
            "LibreriaProductos",
            convenciones,
            tipo => tipo.Namespace?.StartsWith(
                "Libreria.Servicio.Productos",
                StringComparison.Ordinal) == true);
    }
}
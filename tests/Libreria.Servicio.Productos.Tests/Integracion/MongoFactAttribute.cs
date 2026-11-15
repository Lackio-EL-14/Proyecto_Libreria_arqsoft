namespace Libreria.Servicio.Productos.Tests.Integracion;

public sealed class MongoFactAttribute : FactAttribute
{
    public const string Variable = "ConnectionStrings__ProductosDb";

    public MongoFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(
            Environment.GetEnvironmentVariable(Variable)))
        {
            Skip =
                $"Defina {Variable} para correr las pruebas contra MongoDB.";
        }
    }
}
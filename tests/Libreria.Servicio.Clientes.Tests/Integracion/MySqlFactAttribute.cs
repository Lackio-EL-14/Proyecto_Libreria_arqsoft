namespace Libreria.Servicio.Clientes.Tests.Integracion;

/// <summary>
/// Prueba contra el contenedor <c>clientes-db</c> real. Se omite si no está definida
/// la variable <c>ConnectionStrings__ClientesDb</c> (la misma que usa el servicio).
/// </summary>
public sealed class MySqlFactAttribute : FactAttribute
{
    public const string Variable = "ConnectionStrings__ClientesDb";

    public MySqlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(Variable)))
        {
            Skip = $"Defina {Variable} para correr las pruebas contra MySQL (docker compose up -d clientes-db).";
        }
    }
}

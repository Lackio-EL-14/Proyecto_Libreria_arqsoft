namespace Libreria.Servicio.Reportes.Infrastructure.Connection;

// Copia independiente del enfoque de Servicio.Ventas; almacena configuración, no conexiones abiertas.
public sealed class ConnectionStringSingleton
{
    private static readonly Lazy<ConnectionStringSingleton> LazyInstance =
        new(() => new ConnectionStringSingleton(), LazyThreadSafetyMode.ExecutionAndPublication);
    private readonly object _lock = new();
    private string? _connectionString;

    private ConnectionStringSingleton() { }
    public static ConnectionStringSingleton Instancia => LazyInstance.Value;
    public string LibreriaDb => _connectionString
        ?? throw new InvalidOperationException("Falta configurar ConnectionStrings__LibreriaDb.");

    public void Configurar(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        lock (_lock)
        {
            if (_connectionString is not null &&
                !string.Equals(_connectionString, connectionString, StringComparison.Ordinal))
                throw new InvalidOperationException("La cadena de conexión ya fue configurada.");
            _connectionString ??= connectionString;
        }
    }
}

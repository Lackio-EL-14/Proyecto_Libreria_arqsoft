namespace Libreria.Servicio.Usuarios.Infrastructure.Connection;

public sealed class ConnectionStringSingleton
{
    private static readonly Lazy<ConnectionStringSingleton> LazyInstance =
        new(
            () => new ConnectionStringSingleton(),
            LazyThreadSafetyMode.ExecutionAndPublication);

    private readonly object _lock = new();

    private string? _libraryDb;

    private ConnectionStringSingleton()
    {
    }

    public static ConnectionStringSingleton Instance =>
        LazyInstance.Value;

    public string LibraryDb =>
        _libraryDb
        ?? throw new InvalidOperationException(
            "La cadena de conexión 'LibreriaDb' no fue configurada.");

    public void Configure(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException(
                "La cadena de conexión no puede estar vacía.",
                nameof(connectionString));
        }

        lock (_lock)
        {
            if (_libraryDb is not null &&
                !string.Equals(
                    _libraryDb,
                    connectionString,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "La cadena de conexión ya fue configurada.");
            }

            _libraryDb ??= connectionString;
        }
    }
}
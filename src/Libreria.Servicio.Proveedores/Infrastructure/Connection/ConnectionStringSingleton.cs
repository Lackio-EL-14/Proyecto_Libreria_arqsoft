namespace Libreria.Servicio.Proveedores.Infrastructure.Connection;

public sealed class ConnectionStringSingleton
{
    private static readonly Lazy<ConnectionStringSingleton> LazyInstance =
        new(() => new ConnectionStringSingleton(), LazyThreadSafetyMode.ExecutionAndPublication);
    private readonly object _bloqueo = new();
    private string? _proveedoresDb;

    private ConnectionStringSingleton()
    {
    }

    public static ConnectionStringSingleton Instancia => LazyInstance.Value;

    public string ProveedoresDb => _proveedoresDb
        ?? throw new InvalidOperationException("La cadena ProveedoresDb no fue configurada.");

    public void Configurar(string cadena)
    {
        if (string.IsNullOrWhiteSpace(cadena))
        {
            throw new ArgumentException("La cadena no puede estar vacía.", nameof(cadena));
        }

        lock (_bloqueo)
        {
            if (_proveedoresDb is not null && !string.Equals(_proveedoresDb, cadena, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("La cadena de conexión ya fue configurada.");
            }

            _proveedoresDb ??= cadena;
        }
    }
}

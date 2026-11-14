namespace Libreria.Servicio.Clientes.Infrastructure.Connection;

/// <summary>
/// Singleton (copia del de Ventas): guarda la cadena de conexión de la BD de Clientes.
/// Se configura una sola vez al arrancar; un segundo valor distinto es un error.
/// </summary>
public sealed class ConnectionStringSingleton
{
    private static readonly Lazy<ConnectionStringSingleton> LazyInstance =
        new(() => new ConnectionStringSingleton(), LazyThreadSafetyMode.ExecutionAndPublication);

    private readonly object _bloqueo = new();
    private string? _clientesDb;

    private ConnectionStringSingleton()
    {
    }

    public static ConnectionStringSingleton Instancia => LazyInstance.Value;

    public string ClientesDb => _clientesDb
        ?? throw new InvalidOperationException("La cadena de conexión 'ClientesDb' no fue configurada.");

    public void Configurar(string cadenaConexion)
    {
        if (string.IsNullOrWhiteSpace(cadenaConexion))
        {
            throw new ArgumentException("La cadena de conexión no puede estar vacía.", nameof(cadenaConexion));
        }

        lock (_bloqueo)
        {
            if (_clientesDb is not null
                && !string.Equals(_clientesDb, cadenaConexion, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("La cadena de conexión ya fue configurada.");
            }

            _clientesDb ??= cadenaConexion;
        }
    }
}

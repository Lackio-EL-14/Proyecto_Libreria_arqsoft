using System.Threading;

namespace Libreria.Servicio.Ventas.Infrastructure;

public sealed class ConnectionStringSingleton
{
    private static readonly Lazy<ConnectionStringSingleton> LazyInstance =
        new(() => new ConnectionStringSingleton(), LazyThreadSafetyMode.ExecutionAndPublication);

    private readonly object _bloqueo = new();
    private string? _ventasDb;

    private ConnectionStringSingleton()
    {
    }

    public static ConnectionStringSingleton Instancia => LazyInstance.Value;

    public string VentasDb => _ventasDb
        ?? throw new InvalidOperationException("La cadena de conexión 'VentasDb' no fue configurada.");

    public void Configurar(string cadenaConexion)
    {
        if (string.IsNullOrWhiteSpace(cadenaConexion))
        {
            throw new ArgumentException("La cadena de conexión no puede estar vacía.", nameof(cadenaConexion));
        }

        lock (_bloqueo)
        {
            if (_ventasDb is not null
                && !string.Equals(_ventasDb, cadenaConexion, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("La cadena de conexión ya fue configurada.");
            }

            _ventasDb ??= cadenaConexion;
        }
    }
}

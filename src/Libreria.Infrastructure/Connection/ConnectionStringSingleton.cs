using System;
using System.Threading;

namespace Libreria.Infrastructure;

// TEMPORAL (puente): el módulo de Usuarios sigue viviendo en estas bibliotecas
// hasta que pase a Libreria.Servicio.Usuarios (US-92). Cuando eso ocurra,
// esta carpeta Connection se elimina junto con las bibliotecas antiguas.
public sealed class ConnectionStringSingleton
{
    private static readonly Lazy<ConnectionStringSingleton> LazyInstance =
        new(() => new ConnectionStringSingleton(), LazyThreadSafetyMode.ExecutionAndPublication);

    private readonly object _lock = new();
    private string? _libreriaDb;

    private ConnectionStringSingleton()
    {
    }

    public static ConnectionStringSingleton Instancia => LazyInstance.Value;

    public string LibreriaDb => _libreriaDb
        ?? throw new InvalidOperationException("La cadena de conexión 'LibreriaDb' no fue configurada.");

    public void Configurar(string cadenaConexion)
    {
        if (string.IsNullOrWhiteSpace(cadenaConexion))
        {
            throw new ArgumentException("La cadena de conexión no puede estar vacía.", nameof(cadenaConexion));
        }

        lock (_lock)
        {
            if (_libreriaDb is not null &&
                !string.Equals(_libreriaDb, cadenaConexion, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("La cadena de conexión ya fue configurada.");
            }

            _libreriaDb ??= cadenaConexion;
        }
    }
}

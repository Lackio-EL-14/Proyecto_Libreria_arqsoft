namespace Libreria.Servicio.Usuarios.Infrastructure.Identidad;

/// <summary>
/// Guarda el token de la cuenta de servicio hasta poco antes de que venza, para no pedir
/// uno por cada llamada. Se registra como singleton; el token nunca se escribe en logs.
/// </summary>
public sealed class CacheTokenKeycloak
{
    private static readonly TimeSpan Margen = TimeSpan.FromSeconds(30);

    private readonly SemaphoreSlim _candado = new(1, 1);
    private readonly TimeProvider _tiempo;
    private string? _token;
    private DateTimeOffset _vence;

    public CacheTokenKeycloak(TimeProvider tiempo)
    {
        _tiempo = tiempo;
    }

    public async Task<string> ObtenerAsync(
        Func<CancellationToken, Task<(string Token, int ExpiraEnSegundos)>> pedirToken,
        CancellationToken cancellationToken)
    {
        if (TokenVigente() is { } vigente)
        {
            return vigente;
        }

        await _candado.WaitAsync(cancellationToken);
        try
        {
            if (TokenVigente() is { } otroVigente)
            {
                return otroVigente;
            }

            var (token, expiraEn) = await pedirToken(cancellationToken);
            _token = token;
            _vence = _tiempo.GetUtcNow().AddSeconds(expiraEn) - Margen;
            return token;
        }
        finally
        {
            _candado.Release();
        }
    }

    public void Invalidar() => _token = null;

    private string? TokenVigente() =>
        _token is not null && _tiempo.GetUtcNow() < _vence ? _token : null;
}

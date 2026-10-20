using Libreria.Application.Caching;
using Libreria.Application.Models;
using Libreria.Application.Ports.Secondary;
using Microsoft.Extensions.Logging;

namespace Libreria.Application.Services;

public sealed class CacheDiagnosticoService
{
    private static readonly TimeSpan ExpiracionDiagnostico = TimeSpan.FromDays(1);

    private readonly ICacheStore _cacheStore;
    private readonly ILogger<CacheDiagnosticoService> _logger;
    private CacheDiagnostico? _ultimoLocal;

    public CacheDiagnosticoService(
        ICacheStore cacheStore,
        ILogger<CacheDiagnosticoService> logger)
    {
        _cacheStore = cacheStore;
        _logger = logger;
    }

    public async Task RegistrarAsync(CacheDiagnostico diagnostico)
    {
        _ultimoLocal = diagnostico;

        try
        {
            await _cacheStore.GuardarAsync(
                CacheKeys.UltimoDiagnostico,
                diagnostico,
                ExpiracionDiagnostico);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "No fue posible compartir el último diagnóstico de caché en Redis");
        }
    }

    public async Task<CacheDiagnostico?> ObtenerUltimoAsync()
    {
        try
        {
            return await _cacheStore.ObtenerAsync<CacheDiagnostico>(
                CacheKeys.UltimoDiagnostico) ?? _ultimoLocal;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "No fue posible leer el diagnóstico compartido; se usará el último resultado local");

            return _ultimoLocal;
        }
    }
}

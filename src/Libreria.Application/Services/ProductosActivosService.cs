using System.Diagnostics;
using Libreria.Application.Caching;
using Libreria.Application.Models;
using Libreria.Application.Ports.Secondary;
using Libreria.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libreria.Application.Services;

public sealed class ProductosActivosService
{
    private readonly ICacheStore _cacheStore;
    private readonly ILogger<ProductosActivosService> _logger;
    private readonly CacheDiagnosticoService _diagnosticoService;
    private readonly TimeSpan _expiracionCache;

    public ProductosActivosService(
        ICacheStore cacheStore,
        ILogger<ProductosActivosService> logger,
        CacheDiagnosticoService diagnosticoService,
        IOptions<ProductosActivosCacheOptions> options)
    {
        _cacheStore = cacheStore;
        _logger = logger;
        _diagnosticoService = diagnosticoService;
        _expiracionCache = TimeSpan.FromMinutes(options.Value.ExpiracionMinutos);
    }

    public async Task<IReadOnlyList<Producto>> ObtenerAsync(
        ICrudRepository<Producto> repository)
    {
        var cronometro = Stopwatch.StartNew();

        try
        {
            var productosCacheados = await _cacheStore.ObtenerAsync<List<Producto>>(
                CacheKeys.ProductosActivos);

            if (productosCacheados is not null)
            {
                var ttl = await ObtenerTiempoVidaSeguroAsync();
                cronometro.Stop();
                await RegistrarResultadoAsync(
                    "HIT",
                    "Redis",
                    ttl,
                    cronometro.Elapsed.TotalMilliseconds);

                return productosCacheados;
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Redis no está disponible para leer {CacheKey}; se consultará la base de datos",
                CacheKeys.ProductosActivos);

            var productosSinRedis = await repository.ObtenerActivasAsync();
            cronometro.Stop();
            await RegistrarResultadoAsync(
                "MISS",
                "SQL",
                null,
                cronometro.Elapsed.TotalMilliseconds);

            return productosSinRedis;
        }

        var productos = await repository.ObtenerActivasAsync();

        try
        {
            await _cacheStore.GuardarAsync(
                CacheKeys.ProductosActivos,
                productos,
                _expiracionCache);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Redis no está disponible para guardar {CacheKey}; se devolverá el resultado de la base de datos",
                CacheKeys.ProductosActivos);
        }

        var tiempoVida = await ObtenerTiempoVidaSeguroAsync();
        cronometro.Stop();
        await RegistrarResultadoAsync(
            "MISS",
            "SQL",
            tiempoVida,
            cronometro.Elapsed.TotalMilliseconds);

        return productos;
    }

    public async Task InvalidarAsync()
    {
        try
        {
            await _cacheStore.EliminarAsync(CacheKeys.ProductosActivos);

            _logger.LogInformation(
                "Cache invalidada Clave={CacheKey} Replica={Replica}",
                CacheKeys.ProductosActivos,
                Environment.MachineName);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Redis no está disponible para eliminar {CacheKey}",
                CacheKeys.ProductosActivos);
        }
    }

    private async Task<TimeSpan?> ObtenerTiempoVidaSeguroAsync()
    {
        try
        {
            return await _cacheStore.ObtenerTiempoVidaAsync(
                CacheKeys.ProductosActivos);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "No fue posible consultar el TTL de {CacheKey}",
                CacheKeys.ProductosActivos);

            return null;
        }
    }

    private async Task RegistrarResultadoAsync(
        string resultado,
        string origen,
        TimeSpan? tiempoVida,
        double milisegundos)
    {
        var diagnostico = new CacheDiagnostico(
            resultado,
            CacheKeys.ProductosActivos,
            Environment.MachineName,
            origen,
            tiempoVida.HasValue ? Math.Max(0, (long)tiempoVida.Value.TotalSeconds) : null,
            Math.Round(milisegundos, 2),
            DateTimeOffset.UtcNow);

        _logger.LogInformation(
            "Cache {Resultado} Clave={CacheKey} Replica={Replica} Origen={Origen} TTL={TtlSegundos} DuracionMs={DuracionMs}",
            diagnostico.Resultado,
            diagnostico.Clave,
            diagnostico.Replica,
            diagnostico.Origen,
            diagnostico.TtlSegundos,
            diagnostico.Milisegundos);

        await _diagnosticoService.RegistrarAsync(diagnostico);
    }
}

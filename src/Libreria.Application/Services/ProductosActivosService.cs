using Libreria.Application.Caching;
using Libreria.Application.Ports.Secondary;
using Libreria.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libreria.Application.Services;

public sealed class ProductosActivosService
{
    private readonly ICacheStore _cacheStore;
    private readonly ILogger<ProductosActivosService> _logger;
    private readonly TimeSpan _expiracionCache;

    public ProductosActivosService(
        ICacheStore cacheStore,
        ILogger<ProductosActivosService> logger,
        IOptions<ProductosActivosCacheOptions> options)
    {
        _cacheStore = cacheStore;
        _logger = logger;
        _expiracionCache = TimeSpan.FromMinutes(options.Value.ExpiracionMinutos);
    }

    public async Task<IReadOnlyList<Producto>> ObtenerAsync(
        ICrudRepository<Producto> repository)
    {
        try
        {
            var productosCacheados = await _cacheStore.ObtenerAsync<List<Producto>>(
                CacheKeys.ProductosActivos);

            if (productosCacheados is not null)
            {
                _logger.LogInformation(
                    "Cache HIT para {CacheKey}",
                    CacheKeys.ProductosActivos);

                return productosCacheados;
            }

            _logger.LogInformation(
                "Cache MISS para {CacheKey}",
                CacheKeys.ProductosActivos);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Redis no está disponible para leer {CacheKey}; se consultará la base de datos",
                CacheKeys.ProductosActivos);

            return await repository.ObtenerActivasAsync();
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

        return productos;
    }

    public async Task InvalidarAsync()
    {
        try
        {
            await _cacheStore.EliminarAsync(CacheKeys.ProductosActivos);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Redis no está disponible para eliminar {CacheKey}",
                CacheKeys.ProductosActivos);
        }
    }
}

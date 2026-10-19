using System.Text.Json;
using Libreria.Application.Ports.Secondary;
using Microsoft.Extensions.Caching.Distributed;

namespace Libreria.Infrastructure.Caching;

public sealed class RedisCacheStore : ICacheStore
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    private readonly IDistributedCache _cache;

    public RedisCacheStore(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<T?> ObtenerAsync<T>(
        string clave,
        CancellationToken cancellationToken = default)
    {
        var contenido = await _cache.GetAsync(clave, cancellationToken);

        return contenido is null
            ? default
            : JsonSerializer.Deserialize<T>(contenido, SerializerOptions);
    }

    public Task GuardarAsync<T>(
        string clave,
        T valor,
        TimeSpan expiracion,
        CancellationToken cancellationToken = default)
    {
        var contenido = JsonSerializer.SerializeToUtf8Bytes(valor, SerializerOptions);
        var opciones = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiracion
        };

        return _cache.SetAsync(clave, contenido, opciones, cancellationToken);
    }

    public Task EliminarAsync(
        string clave,
        CancellationToken cancellationToken = default)
    {
        return _cache.RemoveAsync(clave, cancellationToken);
    }
}

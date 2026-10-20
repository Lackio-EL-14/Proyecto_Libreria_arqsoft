using System.Text.Json;
using Libreria.Application.Ports.Secondary;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace Libreria.Infrastructure.Caching;

public sealed class RedisCacheStore : ICacheStore, IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    private readonly IDistributedCache _cache;
    private readonly Lazy<ConnectionMultiplexer> _connection;
    private readonly string _instanceName;

    public RedisCacheStore(
        IDistributedCache cache,
        IConfiguration configuration)
    {
        _cache = cache;
        _instanceName = configuration["Redis:InstanceName"]
            ?? configuration["REDIS_INSTANCE_NAME"]
            ?? "libreria:";

        var host = configuration["Redis:Host"]
            ?? configuration["REDIS_HOST"]
            ?? throw new InvalidOperationException("Falta la configuración Redis:Host.");

        var connectionOptions = ConfigurationOptions.Parse(host);
        connectionOptions.AbortOnConnectFail = false;
        connectionOptions.ConnectRetry = 1;
        connectionOptions.ConnectTimeout = 1000;

        _connection = new Lazy<ConnectionMultiplexer>(
            () => ConnectionMultiplexer.Connect(connectionOptions),
            LazyThreadSafetyMode.ExecutionAndPublication);
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

    public async Task<TimeSpan?> ObtenerTiempoVidaAsync(
        string clave,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _connection.Value
            .GetDatabase()
            .KeyTimeToLiveAsync($"{_instanceName}{clave}");
    }

    public void Dispose()
    {
        if (_connection.IsValueCreated)
        {
            _connection.Value.Dispose();
        }
    }
}

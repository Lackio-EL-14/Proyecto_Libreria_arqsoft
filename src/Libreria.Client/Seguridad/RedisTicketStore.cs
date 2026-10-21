using System.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Libreria.Client.Seguridad;

public sealed class RedisTicketStore : ITicketStore
{
    private const string KeyPrefix = "sesiones:";
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisTicketStore> _logger;

    public RedisTicketStore(
        IDistributedCache cache,
        ILogger<RedisTicketStore> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var key = Guid.NewGuid().ToString("N");
        await GuardarAsync(key, ticket, "CREADA");
        return key;
    }

    public Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        return GuardarAsync(key, ticket, "RENOVADA");
    }

    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        var cronometro = Stopwatch.StartNew();

        try
        {
            var bytes = await _cache.GetAsync(ClaveRedis(key));
            cronometro.Stop();

            _logger.LogInformation(
                "Sesion Redis LECTURA Id={SessionId} Replica={Replica} Encontrada={Encontrada} DuracionMs={DuracionMs}",
                IdSeguro(key),
                Environment.MachineName,
                bytes is not null,
                Math.Round(cronometro.Elapsed.TotalMilliseconds, 2));

            return bytes is null
                ? null
                : TicketSerializer.Default.Deserialize(bytes);
        }
        catch (Exception exception)
        {
            cronometro.Stop();
            _logger.LogWarning(
                exception,
                "Redis no respondio al recuperar la sesion Id={SessionId} Replica={Replica} DuracionMs={DuracionMs}",
                IdSeguro(key),
                Environment.MachineName,
                Math.Round(cronometro.Elapsed.TotalMilliseconds, 2));

            return null;
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _cache.RemoveAsync(ClaveRedis(key));
            _logger.LogInformation(
                "Sesion Redis ELIMINADA Id={SessionId} Replica={Replica}",
                IdSeguro(key),
                Environment.MachineName);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Redis no respondio al eliminar la sesion Id={SessionId} Replica={Replica}",
                IdSeguro(key),
                Environment.MachineName);
        }
    }

    private async Task GuardarAsync(
        string key,
        AuthenticationTicket ticket,
        string operacion)
    {
        var cronometro = Stopwatch.StartNew();
        var options = new DistributedCacheEntryOptions();

        if (ticket.Properties.ExpiresUtc is { } expiresUtc)
        {
            options.SetAbsoluteExpiration(expiresUtc);
        }
        else
        {
            options.SetSlidingExpiration(TimeSpan.FromHours(8));
        }

        try
        {
            var contenido = TicketSerializer.Default.Serialize(ticket);
            await _cache.SetAsync(ClaveRedis(key), contenido, options);
            cronometro.Stop();

            _logger.LogInformation(
                "Sesion Redis {Operacion} Id={SessionId} Replica={Replica} DuracionMs={DuracionMs}",
                operacion,
                IdSeguro(key),
                Environment.MachineName,
                Math.Round(cronometro.Elapsed.TotalMilliseconds, 2));
        }
        catch (Exception exception)
        {
            cronometro.Stop();
            _logger.LogError(
                exception,
                "Redis no respondio al guardar la sesion Id={SessionId} Replica={Replica} DuracionMs={DuracionMs}",
                IdSeguro(key),
                Environment.MachineName,
                Math.Round(cronometro.Elapsed.TotalMilliseconds, 2));
            throw;
        }
    }

    private static string ClaveRedis(string key)
    {
        return $"{KeyPrefix}{key}";
    }

    private static string IdSeguro(string key)
    {
        return key.Length <= 8 ? key : key[^8..];
    }
}

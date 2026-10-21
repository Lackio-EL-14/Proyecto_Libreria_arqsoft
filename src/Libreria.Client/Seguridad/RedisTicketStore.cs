using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Distributed;

namespace Libreria.Client.Seguridad;

public class RedisTicketStore : ITicketStore
{
    private const string KeyPrefix = "LibreriaAuth:";
    private readonly IDistributedCache _cache;

    public RedisTicketStore(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var key = Guid.NewGuid().ToString();
        await RenewAsync(key, ticket);
        return key;
    }

    public async Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        var options = new DistributedCacheEntryOptions();
        var expiresUtc = ticket.Properties.ExpiresUtc;
        
        if (expiresUtc.HasValue)
            options.SetAbsoluteExpiration(expiresUtc.Value);
        else
            options.SetSlidingExpiration(TimeSpan.FromHours(8));

        byte[] val = TicketSerializer.Default.Serialize(ticket);
        await _cache.SetAsync(KeyPrefix + key, val, options);
    }

    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        byte[]? bytes = await _cache.GetAsync(KeyPrefix + key);
        return bytes is null ? null : TicketSerializer.Default.Deserialize(bytes);
    }

    public async Task RemoveAsync(string key)
    {
        await _cache.RemoveAsync(KeyPrefix + key);
    }
}

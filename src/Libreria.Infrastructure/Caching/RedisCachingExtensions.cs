using Libreria.Application.Ports.Secondary;
using Libreria.Infrastructure.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Libreria.Infrastructure;

public static class RedisCachingExtensions
{
    public static IServiceCollection AddRedisCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var host = configuration["Redis:Host"]
            ?? configuration["REDIS_HOST"]
            ?? throw new InvalidOperationException("Falta la configuración Redis:Host.");

        var instanceName = configuration["Redis:InstanceName"]
            ?? configuration["REDIS_INSTANCE_NAME"]
            ?? "libreria:";

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = host;
            options.InstanceName = instanceName;
        });

        services.AddSingleton<ICacheStore, RedisCacheStore>();

        return services;
    }
}

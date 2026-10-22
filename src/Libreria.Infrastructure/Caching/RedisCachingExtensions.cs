using Libreria.Application.Ports.Secondary;
using Libreria.Infrastructure.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

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

        var connectTimeout = configuration.GetValue(
            "Redis:ConnectTimeoutMilliseconds",
            1_000);
        var operationTimeout = configuration.GetValue(
            "Redis:OperationTimeoutMilliseconds",
            1_000);

        var connectionOptions = ConfigurationOptions.Parse(host);
        connectionOptions.AbortOnConnectFail = false;
        connectionOptions.ConnectRetry = 2;
        connectionOptions.ConnectTimeout = connectTimeout;
        connectionOptions.AsyncTimeout = operationTimeout;
        connectionOptions.SyncTimeout = operationTimeout;
        connectionOptions.ResolveDns = false;
        connectionOptions.ClientName = Environment.MachineName;

        var connection = new Lazy<IConnectionMultiplexer>(
            () => ConnectionMultiplexer.Connect(connectionOptions),
            LazyThreadSafetyMode.ExecutionAndPublication);

        services.AddSingleton<IConnectionMultiplexer>(_ => connection.Value);

        services.AddStackExchangeRedisCache(options =>
        {
            options.ConnectionMultiplexerFactory = () =>
                Task.FromResult(connection.Value);
            options.InstanceName = instanceName;
        });

        services.AddSingleton<ICacheStore, RedisCacheStore>();

        return services;
    }
}

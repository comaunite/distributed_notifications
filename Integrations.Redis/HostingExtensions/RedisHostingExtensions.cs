using Integrations.Redis.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Persistence.Stores;
using StackExchange.Redis;

namespace Integrations.Redis.HostingExtensions;

public static class RedisHostingExtensions
{
    public static IHostApplicationBuilder AddRedis(this IHostApplicationBuilder builder)
    {
        var multiplexer = ConnectionMultiplexer.Connect(builder.Configuration["REDIS:HOST"]!);

        builder.Services.AddSingleton<IConnectionMultiplexer>(multiplexer);

        builder.Services.AddSingleton<IDeduplicationStore, RedisDeduplicationStore>();

        builder.Services.Decorate<INotificationStore, RedisNotificationStore>();

        return builder;
    }
}
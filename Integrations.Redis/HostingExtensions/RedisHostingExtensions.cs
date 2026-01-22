using System.Diagnostics.CodeAnalysis;
using Integrations.Redis.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Persistence.Stores;
using StackExchange.Redis;

namespace Integrations.Redis.HostingExtensions;

[SuppressMessage("ReSharper", "ConvertToExtensionBlock")]
public static class RedisHostingExtensions
{
    public static IHostApplicationBuilder AddRedis(this IHostApplicationBuilder builder)
    {
        var multiplexer = ConnectionMultiplexer.Connect(builder.Configuration["REDIS:HOST"]!);

        builder.Services.AddSingleton<IConnectionMultiplexer>(multiplexer);

        builder.Services.AddSingleton<IDeduplicationStore, RedisDeduplicationStore>();

        return builder;
    }

    public static IHostApplicationBuilder WithPersistenceStoreDecorators(this IHostApplicationBuilder builder)
    {
        builder.Services.Decorate<INotificationStore, RedisNotificationStore>();

        return builder;
    }
}
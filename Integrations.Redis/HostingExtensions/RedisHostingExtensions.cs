using Integrations.Redis.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Persistence.Stores;

namespace Integrations.Redis.HostingExtensions;

public static class RedisHostingExtensions
{
    public static IHostApplicationBuilder AddRedis(this IHostApplicationBuilder builder)
    {
        builder.Services.Decorate<INotificationStore, RedisNotificationStore>();

        return builder;
    }
}
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Integrations.RabbitMQ.HostingExtensions;

[SuppressMessage("ReSharper", "ConvertToExtensionBlock")]
public static class RabbitMqHostingExtensions
{
    public static IHostApplicationBuilder AddRabbitMq<TTopology>(this IHostApplicationBuilder builder)
        where TTopology : class, IHostedService
    {
        builder.Services.AddSingleton<RabbitMqConnectionFactory>();
        builder.Services.AddSingleton<RabbitMqChannelPool>();
        builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();

        // Topology (Exchange/Queue declarations)
        builder.Services.AddHostedService<TTopology>();

        return builder;
    }

    public static IHostApplicationBuilder AddRabbitMq<TTopology, THandler>(this IHostApplicationBuilder builder)
        where THandler : class, IMessageHandler
        where TTopology : class, IHostedService
    {
        builder.AddRabbitMq<TTopology>();

        // Consumer Service
        builder.Services.AddSingleton<THandler>();
        builder.Services.AddHostedService<QueueConsumerService<THandler>>();

        return builder;
    }
}
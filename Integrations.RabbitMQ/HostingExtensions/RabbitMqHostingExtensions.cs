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
        builder.Services.AddSingleton<RabbitMqPublisherChannelPool>();
        builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();

        // Topology (Exchange/Queue declarations)
        builder.Services.AddHostedService<TTopology>();

        return builder;
    }

    public static IHostApplicationBuilder AddRabbitMq<TTopology, THandler>(this IHostApplicationBuilder builder, Action<QueueConsumerOptions> options)
        where THandler : class, IMessageHandler
        where TTopology : class, IHostedService
    {
        builder.AddRabbitMq<TTopology>();

        builder.Services.Configure(options);

        // Consumer Service
        builder.Services.AddSingleton<THandler>();
        builder.Services.AddHostedService<QueueConsumerService<THandler>>();

        return builder;
    }
}

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public sealed class QueueConsumerOptions
{
    public uint PrefetchSize { get; set; }
    public ushort PrefetchCount { get; set; } = 10;
    public bool GlobalQos { get; set; }
    public bool AutoAck { get; set; }
    public string? ListeningQueueName { get; set; }
}
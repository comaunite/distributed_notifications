using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Integrations.RabbitMQ.HostingExtensions;

[SuppressMessage("ReSharper", "ConvertToExtensionBlock")]
public static class RabbitMqHostingExtensions
{
    public static IHostApplicationBuilder AddRabbitMqPublisher<TTopology>(this IHostApplicationBuilder builder,
        Action<RabbitMqPublisherChannelPoolOptions> publisherChannelPoolOptionsBuilder)
        where TTopology : class, IHostedService
    {
        builder.Services.Configure(publisherChannelPoolOptionsBuilder);

        builder.Services.AddSingleton<RabbitMqConnectionFactory>();
        builder.Services.AddSingleton<RabbitMqPublisherChannelPool>();
        builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();

        // Topology (Exchange/Queue declarations)
        builder.Services.AddHostedService<TTopology>();

        builder.Services.AddHostedService<RabbitMqWarmupService>();

        return builder;
    }

    public static IHostApplicationBuilder AddRabbitMqListenerAndPublisher<TTopology, THandler>(this IHostApplicationBuilder builder,
        Action<QueueConsumerOptions> consumerOptionsBuilder, Action<RabbitMqPublisherChannelPoolOptions> publisherChannelPoolOptionsBuilder)
        where THandler : class, IMessageHandler
        where TTopology : class, IHostedService
    {
        builder.Services.Configure(consumerOptionsBuilder);

        builder.AddRabbitMqPublisher<TTopology>(publisherChannelPoolOptionsBuilder);

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
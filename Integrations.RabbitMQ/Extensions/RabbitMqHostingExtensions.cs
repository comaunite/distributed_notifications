using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Integrations.RabbitMQ.Extensions;

[SuppressMessage("ReSharper", "ConvertToExtensionBlock")]
public static class RabbitMqHostingExtensions
{
    public static IHostApplicationBuilder AddRabbitMq<TTopology>(this IHostApplicationBuilder builder)
        where TTopology : class, IHostedService
    {
        builder.Services.Configure<RabbitMqConnectionOptions>(options =>
        {
            options.HostName =  builder.Configuration["RABBITMQ:HOST"] ?? options.HostName;

            if (int.TryParse( builder.Configuration["RABBITMQ:PORT"], out var port))
                options.Port = port;

            options.UserName =  builder.Configuration["RABBITMQ:USERNAME"] ?? options.UserName;
            options.Password =  builder.Configuration["RABBITMQ:PASSWORD"] ?? options.Password;
        });

        builder.Services.AddSingleton<RabbitMqConnectionFactory>();

        builder.Services.AddHostedService<TTopology>();

        return builder;
    }

    public static IHostApplicationBuilder AddRabbitMqPublisher(this IHostApplicationBuilder builder,
        Action<RabbitMqPublisherChannelPoolOptions> publisherChannelPoolOptionsBuilder)
    {
        builder.Services.Configure(publisherChannelPoolOptionsBuilder);
        builder.Services.AddSingleton<RabbitMqPublisherChannelPool>();
        builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
        builder.Services.AddHostedService<RabbitMqChannelPoolWarmupService>();

        return builder;
    }

    public static IHostApplicationBuilder AddRabbitMqListener<THandler>(this IHostApplicationBuilder builder,
        Action<QueueConsumerOptions> consumerOptionsBuilder)
        where THandler : class, IMessageHandler
    {
        builder.Services.Configure(consumerOptionsBuilder);
        builder.Services.AddScoped<THandler>();
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

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public class RabbitMqConnectionOptions
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";

    public bool AutomaticRecoveryEnabled { get; set; } = true;
    public TimeSpan NetworkRecoveryInterval { get; set; } = TimeSpan.FromSeconds(5);

    public int MaxRetryAttempts { get; set; } = 5;
    public int RetryDelaySeconds { get; set; } = 1;
}
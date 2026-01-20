using System.Diagnostics.CodeAnalysis;
using Integrations.RabbitMQ.HostingExtensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Integrations.RabbitMQ;

public interface IMessageHandler
{
    ValueTask<(bool success, string? error)> ProcessAsync(ReadOnlyMemory<byte> body, string? correlationId, CancellationToken cancellationToken);
}

public sealed class QueueConsumerService<T>(RabbitMqConnectionFactory connectionFactory, T handler, IOptions<QueueConsumerOptions> options,
    ILogger<QueueConsumerService<T>> logger)
    : BackgroundService
    where T : class, IMessageHandler
{
    private IChannel? channel;

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting Queue Consumer Service");

        while (!stoppingToken.IsCancellationRequested)
        {
            EnsureConfigurationValidity();

            try
            {
                await InitializeRabbitMqAsync(stoppingToken);

                // Keep the service alive while the consumer runs in the background
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Queue Consumer Service is stopping.");
                break;
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Fatal error in Queue Consumer Service. The container may restart.");

                // Clean up connections, take a break and try to re-initialize
                await CleanUpAsync();

                await Task.Delay(10000, stoppingToken);
            }
        }
    }

    private void EnsureConfigurationValidity()
    {
        if (string.IsNullOrEmpty(options.Value.ListeningQueueName))
        {
            throw new InvalidOperationException("Queue name is not configured. Please set the QueueName option.");
        }
    }

    private async Task InitializeRabbitMqAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Initializing connection with RabbitMQ...");

        var connection = await connectionFactory.GetConnectionAsync(cancellationToken);
        channel = await connection.CreateChannelAsync(null, cancellationToken);

        await channel.BasicQosAsync(
            prefetchSize: options.Value.PrefetchSize,
            prefetchCount: options.Value.PrefetchCount,
            global: options.Value.GlobalQos,
            cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += OnMessageReceivedAsync;

        await channel.BasicConsumeAsync(
            queue: options.Value.ListeningQueueName!,
            autoAck: options.Value.AutoAck,
            consumer: consumer,
            cancellationToken: cancellationToken);

        logger.LogInformation("Queue Consumer is now listening on {Queue}", options.Value.ListeningQueueName);
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs args)
    {
        if (channel is not { IsOpen: true })
        {
            logger.LogWarning("Received message but channel is closed. Message will be requeued by RabbitMQ.");
            return;
        }

        try
        {
            var result = await handler.ProcessAsync(args.Body, args.BasicProperties.CorrelationId, args.CancellationToken);

            if (!result.success)
            {
                logger.LogError("Message processing failed, requeuing message with CorrelationId: {CorrelationId}. Error: {Error}",
                    args.BasicProperties.CorrelationId, result.error);

                // TODO: Handle permanent errors with requeue = false and DLQ

                await channel!.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: true, cancellationToken: args.CancellationToken);
                return;
            }

            await channel!.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken: args.CancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing message with CorrelationId: {CorrelationId}: {Error}",
                args.BasicProperties.CorrelationId, ex.Message);

            if (channel is { IsOpen: true })
            {
                await channel!.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: true, cancellationToken: args.CancellationToken);
            }
        }
    }

    private async Task CleanUpAsync()
    {
        if (channel != null)
        {
            // This channel is configured for listener, so it should not be returned to the pool
            await channel.CloseAsync(CancellationToken.None);
            await channel.DisposeAsync();
            channel = null;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await CleanUpAsync();
        await base.StopAsync(cancellationToken);
        Dispose();
    }
}
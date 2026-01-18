using System.Diagnostics.CodeAnalysis;
using Integrations.RabbitMQ.Factories;
using Integrations.RabbitMQ.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Integrations.RabbitMQ;

public sealed class QueueConsumerService<T>(IRabbitMqConnectionFactory connectionFactory, T handler,
    ILogger<QueueConsumerService<T>> logger)
    : BackgroundService
    where T : class, IMessageHandler
{
    private IConnection? connection;
    private IChannel? channel;

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting Queue Consumer Service");

        while (!stoppingToken.IsCancellationRequested)
        {
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

    private async Task InitializeRabbitMqAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Connecting to RabbitMQ...");

        connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        channel = await connection.CreateChannelAsync(new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true
            ),
            cancellationToken);

        connection.ConnectionShutdownAsync += (_, args) =>
        {
            logger.LogWarning("RabbitMQ Connection lost: {Reason}", args.ReplyText);
            return Task.CompletedTask;
        };

        if (handler is IPublishingMessageHandler publishingHandler)
        {
            logger.LogInformation("Initializing publisher for handler {HandlerType}", typeof(T).Name);
            publishingHandler.InitPublisher(channel);
        }

        await channel.BasicQosAsync(
            prefetchSize: 0, // Size is not limited
            prefetchCount: 10, // Process up to 10 messages concurrently
            global: false,
            cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += OnMessageReceivedAsync;

        await channel.BasicConsumeAsync(
            queue: handler.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken);

        logger.LogInformation("Queue Consumer is now listening on {Queue}", handler.QueueName);
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
            var result = await handler.ProcessAsync(args.Body, CancellationToken.None);

            if (!result.success)
            {
                logger.LogError("Message processing failed, requeuing message with CorrelationId: {CorrelationId}. Error: {Error}",
                    args.BasicProperties.CorrelationId, result.error);

                // TODO: Handle permanent errors with requeue = false and DLQ

                await channel!.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: true, cancellationToken: CancellationToken.None);
                return;
            }

            await channel!.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken: CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing message with CorrelationId: {CorrelationId}: {Error}",
                args.BasicProperties.CorrelationId, ex.Message);

            if (channel is { IsOpen: true })
            {
                await channel!.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: true, cancellationToken: CancellationToken.None);
            }
        }
    }

    private async Task CleanUpAsync()
    {
        if (channel is { IsOpen: true })
            await channel.CloseAsync(CancellationToken.None);

        if (connection is { IsOpen: true })
            await connection.CloseAsync(CancellationToken.None);

        channel?.Dispose();
        connection?.Dispose();
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await CleanUpAsync();
        Dispose();
    }
}
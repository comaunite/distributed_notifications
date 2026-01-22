using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Common;
using Common.Enums;
using Integrations.RabbitMQ;
using Integrations.RabbitMQ.Models;
using Integrations.RabbitMQ.Models.Base;
using Integrations.RabbitMQ.Models.Interfaces;
using Microsoft.Extensions.Logging;
using Persistence.Models;
using Persistence.Stores;

namespace NotificationOrchestrator.Services;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
internal sealed class OrchestrationHandler(INotificationStore store, IRabbitMqPublisher publisher, ILogger<OrchestrationHandler> logger)
    : IMessageHandler
{
    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    public async Task<(bool success, string? error, bool canRetry)> ProcessAsync(ReadOnlyMemory<byte> body, string? correlationId, CancellationToken cancellationToken)
    {
        var message = JsonSerializer.Deserialize(body.Span,
            Integrations.RabbitMQ.Serialization.NotificationSerializationContext.Default.BaseNotification);

        if (message == null)
        {
            return (false, $"Failed to deserialize notification message. CorrelationId: {correlationId}", false);
        }

        try
        {
            // Logging for debugging purposes only, in production should probably avoid logging every message
            logger.LogInformation("Orchestrating notification with ID '{NotificationId}'", message.NotificationId);

            return await ProcessAndFanOutAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing notification with ID '{NotificationId}'", message.NotificationId);

            return (false, ex.Message, true);
        }
    }

    private async Task<(bool success, string? error, bool canRetry)> ProcessAndFanOutAsync(BaseNotification message, CancellationToken cancellationToken)
    {
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = cancellationToken
        };

        var count = 0;

        await Parallel.ForEachAsync(
            store.GetNotificationRecipientsAsync(message.Type, cancellationToken),
            options,
            async (recipient, ct) =>
            {
                var notification = MapNotification(recipient, message);

                if (notification == null)
                {
                    logger.LogWarning("Failed to map notification {NotificationId} for user {UserId} and channel {Channel}",
                        message.NotificationId, recipient.UserId, recipient.DeliveryChannel);
                }
                else
                {
                    var (success, error) = await publisher.PublishAsync(notification, notification.NotificationId, ct);

                    if (!success)
                    {
                        logger.LogError("Failed to publish {NotificationId} to {Channel} for {UserId}: {Error}",
                            notification.NotificationId, recipient.DeliveryChannel, recipient.UserId, error);
                    }
                }

                Interlocked.Increment(ref count);
            });

        // TODO: Handle partial failures?

        logger.LogInformation("Successfully orchestrated notification {NotificationId} to {RecipientCount} recipients",
            message.NotificationId, count);

        return (true, null, false);
    }

    private static BaseNotification? MapNotification(NotificationRecipient recipient, BaseNotification message)
    {
        var deduplicationId = GuidHelper.CreateDeterministic(
            message.NotificationId,
            recipient.UserId,
            (int)recipient.DeliveryChannel);

        return recipient.DeliveryChannel switch
        {
            DeliveryChannel.Email => CreateNotification(new NotifyEmail(recipient.DeliveryAddress)),
            DeliveryChannel.Sms => CreateNotification(new NotifySms(recipient.DeliveryAddress)),
            DeliveryChannel.Push => CreateNotification(new NotifyPush(recipient.DeliveryAddress)),
            _ => null
        };

        T CreateNotification<T>(T notification)
            where T : BaseNotification, IDeduplicatable, IDeliverableNotification
        {
            notification.NotificationId = message.NotificationId;
            notification.DeduplicationId = deduplicationId;
            notification.Type = message.Type;
            notification.Metadata = message.Metadata;
            notification.CreatedAt = DateTimeOffset.UtcNow;
            return notification;
        }
    }
}
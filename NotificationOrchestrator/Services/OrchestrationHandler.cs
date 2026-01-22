using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Common;
using Common.Enums;
using Integrations.RabbitMQ;
using Integrations.RabbitMQ.Models;
using Integrations.RabbitMQ.Models.Base;
using Microsoft.Extensions.Logging;
using Persistence.Models;
using Persistence.Stores;

namespace NotificationOrchestrator.Services;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
internal sealed class OrchestrationHandler(INotificationStore store, IRabbitMqPublisher publisher, ILogger<OrchestrationHandler> logger) : IMessageHandler
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

        var defaults = await store.GetDefaultPreferencesAsync(message.Type, cancellationToken);

        await Parallel.ForEachAsync(
            store.GetNotificationRecipientsAsync(message.Type, defaults, cancellationToken),
            options,
            async (user, ct) =>
            {
                var publishingTasks = new List<Task>();

                foreach (var channelType in user.EnabledDeliveryChannels)
                {
                    var deduplicationId = GuidHelper.CreateDeterministic(
                        message.NotificationId,
                        user.UserId,
                        (int)channelType);

                    var notification = MapNotification(user, channelType, message, deduplicationId);

                    if (notification == null)
                    {
                        logger.LogWarning("Failed to map notification {NotificationId} for user {UserId} and channel {Channel}",
                            message.NotificationId, user.UserId, channelType);
                        continue;
                    }

                    publishingTasks.Add(HandlePublishAsync(user, notification, channelType, ct));
                }


                await Task.WhenAll(publishingTasks);
            });

        // TODO: Handle partial failures?

        return (true, null, false);
    }

    private async Task HandlePublishAsync<T>(NotificationRecipient recipient, T notification, DeliveryChannel channelType, CancellationToken ct)
        where T : BaseNotification
    {
        var (success, error) = await publisher.PublishAsync(notification, notification.NotificationId, ct);

        if (!success)
        {
            logger.LogError("Failed to publish {NotificationId} to {Channel} for {UserId}: {Error}",
                notification.NotificationId, channelType, recipient.UserId, error);
        }
    }

    private static BaseNotification? MapNotification(NotificationRecipient recipient, DeliveryChannel channelType,
        BaseNotification message, Guid deduplicationId)
    {
        return channelType switch
        {
            DeliveryChannel.Email => new NotifyEmail
            {
                NotificationId = message.NotificationId,
                DeduplicationId = deduplicationId,
                EmailAddress = recipient.Email!,
                Type = NotificationType.NewPost,
                Metadata = message.Metadata,
                CreatedAt = DateTimeOffset.UtcNow
            },
            DeliveryChannel.Sms => new NotifySms
            {
                NotificationId = message.NotificationId,
                DeduplicationId = deduplicationId,
                PhoneNumber = recipient.PhoneNumber!,
                Type = NotificationType.NewPost,
                Metadata = message.Metadata,
                CreatedAt = DateTimeOffset.UtcNow
            },
            DeliveryChannel.Push => new NotifyPush
            {
                NotificationId = message.NotificationId,
                DeduplicationId = deduplicationId,
                DeviceToken = recipient.DeviceToken!,
                Type = NotificationType.NewPost,
                Metadata = message.Metadata,
                CreatedAt = DateTimeOffset.UtcNow
            },
            _ => null
        };
    }
}
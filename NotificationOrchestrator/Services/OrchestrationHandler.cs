using System.Collections.Concurrent;
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
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = cts.Token
        };

        var count = 0;
        var failures = new ConcurrentBag<(Guid UserId, DeliveryChannel Channel, string Error)>();

        // TODO: Consider splitting queries by available delivery channels
        await Parallel.ForEachAsync(
            store.GetNotificationRecipientsAsync(message.Type, cts.Token),
            options,
            async (recipient, ct) =>
            {
                if (failures.Count >= 100)
                {
                    // Cancel further processing if too many failures (arbitrary atm)
                    logger.LogWarning("Aborting notification {NotificationId} processing due to excessive failures", message.NotificationId);
                    await cts.CancelAsync();
                }

                var notification = MapNotification(recipient, message);

                if (notification == null)
                {
                    logger.LogWarning("Failed to map notification {NotificationId} for user {UserId} and channel {Channel}",
                        message.NotificationId, recipient.UserId, recipient.DeliveryChannel);

                    failures.Add((recipient.UserId, recipient.DeliveryChannel, "Mapping failed"));
                }
                else
                {
                    var (success, error) = await publisher.PublishAsync(notification, notification.NotificationId, ct);

                    if (!success)
                    {
                        logger.LogError("Failed to publish {NotificationId} to {Channel} for {UserId}: {Error}",
                            notification.NotificationId, recipient.DeliveryChannel, recipient.UserId, error);

                        failures.Add((recipient.UserId, recipient.DeliveryChannel, error ?? "Unknown error"));
                    }
                }

                Interlocked.Increment(ref count);
            });

        if (!failures.IsEmpty)
        {
            var successCount = count - failures.Count;
            var failureRate = count > 0 ? (double)failures.Count / (count == 0 ? 1 : count) : 0;

            logger.LogWarning(
                "Notification {NotificationId} completed with {SuccessCount}/{TotalCount} successes ({FailureRate:P1} failure rate)",
                message.NotificationId, successCount, count, failureRate);

            // Decide if partial failure should fail the entire operation
            // More than 50% failed
            if (failureRate > 0.5)
            {
                return (false, $"High failure rate: {failures.Count}/{count} recipients failed", false);
            }

            // Failures are likely due to some bug or infrastructure issue
            // There are ways to optimize this further, if we have a partial success.
            // That would help avoid retrying for those messages that were queued successfully (e.g. problem with a specific channel)
            // Can be checked via deduplication id before publishing, but for now we keep it simple.
            return (false, $"Partial failure: {failures.Count}/{count} recipients failed", true);
        }

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
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Integrations.RabbitMQ;
using Microsoft.Extensions.Logging;
using Persistence.Stores;

namespace NotificationWorker.Push.Handlers;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
internal sealed class PushMessageHandler(IDeduplicationStore deduplicationStore, ILogger<PushMessageHandler> logger) : IMessageHandler
{
    public async Task<(bool success, string? error, bool canRetry)> ProcessAsync(ReadOnlyMemory<byte> body, string? correlationId, CancellationToken cancellationToken)
    {
        var message = JsonSerializer.Deserialize(body.Span,
            Integrations.RabbitMQ.Serialization.NotificationSerializationContext.Default.NotifyPush);

        if (message == null)
        {
            return (false, "Failed to deserialize Push notification message", false);
        }

        var deduplicationId = message.DeduplicationId.ToString();

        if (await deduplicationStore.IsDuplicateAsync(deduplicationId))
        {
            logger.LogInformation("Duplicate Push Notification detected: {NotificationId}, skipping processing.", message.NotificationId);

            // Expecting to Ack the message
            return (true, null, false);
        }

        logger.LogInformation("Processing Push Notification: {NotificationId} to {DeviceToken}", message.NotificationId, message.DeviceToken);

        await deduplicationStore.MarkAsProcessedAsync(deduplicationId);

        return (true, null, false);
    }
}
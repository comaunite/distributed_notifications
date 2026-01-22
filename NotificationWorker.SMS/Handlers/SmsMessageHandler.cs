using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Integrations.RabbitMQ;
using Microsoft.Extensions.Logging;
using Persistence.Stores;

namespace NotificationWorker.SMS.Handlers;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
internal sealed class SmsMessageHandler(IDeduplicationStore deduplicationStore, ILogger<SmsMessageHandler> logger) : IMessageHandler
{
    public async Task<(bool success, string? error, bool canRetry)> ProcessAsync(ReadOnlyMemory<byte> body, string? correlationId, CancellationToken cancellationToken)
    {
        var message = JsonSerializer.Deserialize(body.Span,
            Integrations.RabbitMQ.Serialization.NotificationSerializationContext.Default.NotifySms);

        if (message == null)
        {
            return (false, "Failed to deserialize SMS notification message", false);
        }

        var deduplicationId = message.DeduplicationId.ToString();

        if (await deduplicationStore.IsDuplicateAsync(deduplicationId))
        {
            logger.LogInformation("Duplicate SMS Notification detected: {NotificationId}, skipping processing.", message.NotificationId);

            // Expecting to Ack the message
            return (true, null, false);
        }

        logger.LogInformation("Processing SMS Notification: {NotificationId} to {PhoneNumber}", message.NotificationId, message.DeliveryAddress);

        await deduplicationStore.MarkAsProcessedAsync(deduplicationId);

        return (true, null, false);
    }
}